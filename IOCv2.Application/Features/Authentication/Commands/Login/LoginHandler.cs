using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Authentication.Commands.Login
{
    public class LoginHandler : IRequestHandler<LoginCommand, Result<LoginResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordService _passwordService;
        private readonly ITokenService _tokenService;
        private readonly IRateLimiter _rateLimiter;
        private readonly IMessageService _messageService;
        private readonly ILogger<LoginHandler> _logger;

        public LoginHandler(IUnitOfWork unitOfWork, IPasswordService passwordService, ITokenService tokenService, IRateLimiter rateLimiter, IMessageService messageService, ILogger<LoginHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _passwordService = passwordService;
            _tokenService = tokenService;
            _rateLimiter = rateLimiter;
            _messageService = messageService;
            _logger = logger;
        }

        public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            // Each user has own key counting invalid turn
            var rateLimitKey = $"login_attempt:{request.Email}";

            _logger.LogInformation("Login attempt for email: {Email}", request.Email);

            if (await _rateLimiter.IsBlockedAsync(rateLimitKey, cancellationToken))
            {
                _logger.LogWarning("Login blocked for email: {Email} due to rate limiting", request.Email);
                return Result<LoginResponse>.Failure(_messageService.GetMessage(MessageKeys.Auth.AccountBlocked));
            }

            var user = await _unitOfWork.Repository<User>()
                .Query()
                .FirstOrDefaultAsync(e => e.Email == request.Email, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("Login failed: User not found with email: {Email}", request.Email);
                return Result<LoginResponse>.Failure(_messageService.GetMessage(MessageKeys.Auth.InvalidCredentials));
            }

            if (!_passwordService.VerifyPassword(request.Password, user.PasswordHash))
            {
                // Register failed attempt (block after 5 attempts in 15 mins)
                await _rateLimiter.RegisterFailAsync(
                    rateLimitKey,
                    limit: 5,
                    window: TimeSpan.FromMinutes(15),
                    blockFor: TimeSpan.FromMinutes(15),
                    cancellationToken);

                _logger.LogWarning("Login failed: Invalid password for email: {Email}", request.Email);
                return Result<LoginResponse>.Failure(_messageService.GetMessage(MessageKeys.Auth.InvalidCredentials));
            }


            // All good - reset failure count
            await _rateLimiter.ResetAsync(rateLimitKey, cancellationToken);

            // Kiểm tra trạng thái account
            if (user.Status == UserStatus.Inactive)
            {
                _logger.LogWarning("Login failed: Account is inactive for email: {Email}", request.Email);
                return Result<LoginResponse>.Failure(_messageService.GetMessage(MessageKeys.Auth.AccountInactive));
            }

            // Tạo access token
            var accessToken = _tokenService.GenerateAccessToken(user);
            var expiresIn = _tokenService.GetTokenExpirationSeconds();

            // Tạo refresh token
            var refreshToken = _tokenService.GenerateRefreshToken();
            var configDays = _tokenService.GetRefreshTokenExpirationDays();

            // Logic: If RememberMe -> 30 Days (or config * 4). If not -> Config (7 days).
            var expirationDays = request.RememberMe ? 30 : configDays;
            var expirationDate = DateTime.UtcNow.AddDays(expirationDays);

            // Save into db
            var refreshTokenEntity = new Domain.Entities.RefreshToken
            {
                Token = refreshToken,
                Expires = expirationDate,

                UserId = user.UserId
            };

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                // Attach refresh token with user
                await _unitOfWork.Repository<RefreshToken>().AddAsync(refreshTokenEntity, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, "Failed to save refresh token and commit transaction for user: {Email}", request.Email);
                throw;
            }

            // Response Client
            var response = new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                RefreshTokenExpiresIn = (expirationDate - DateTime.UtcNow).TotalSeconds,
                Email = user.Email,
                Role = user.Role,
                ExpiresIn = expiresIn
            };

            _logger.LogInformation("Login successful for email: {Email}", request.Email);
            return Result<LoginResponse>.Success(response);
        }
    }
}
