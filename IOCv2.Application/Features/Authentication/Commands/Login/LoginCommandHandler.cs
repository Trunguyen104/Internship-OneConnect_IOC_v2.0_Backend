using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static IOCv2.Application.Constants.MessageKeys;

namespace IOCv2.Application.Features.Authentication.Commands.Login
{
    public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordService _passwordService;
        private readonly ITokenService _tokenService;
        private readonly IRateLimiter _rateLimiter;
        private readonly IMessageService _messageService;

        public LoginCommandHandler(IUnitOfWork unitOfWork, IPasswordService passwordService, ITokenService tokenService, IRateLimiter rateLimiter, IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _passwordService = passwordService;
            _tokenService = tokenService;
            _rateLimiter = rateLimiter;
            _messageService = messageService;
        }

        public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            // Each user has own key counting invalid turn
            var rateLimitKey = $"login_attempt:{request.Username}";

            if (await _rateLimiter.IsBlockedAsync(rateLimitKey, cancellationToken)) ;
            {
                return Result<LoginResponse>.Failure(_messageService.GetMessage(MessageKeys.Auth.AccountBlocked));
            }

            var user = await _unitOfWork.Repository<User>()
                .Query()
                .FirstOrDefaultAsync(e => e.Username == request.Username, cancellationToken);

            if (user == null)
            {
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

                return Result<LoginResponse>.Failure(_messageService.GetMessage(MessageKeys.Auth.InvalidCredentials));
            }

            // All good - reset failure count
            await _rateLimiter.ResetAsync(rateLimitKey, cancellationToken);

            // Kiểm tra trạng thái account
            if (user.Status == UserStatus.Inactive)
            {
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

                EmployeeId = user.Id
            };

            // Attach refresh token with user
            await _unitOfWork.Repository<RefreshToken>().AddAsync(refreshTokenEntity);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            // Response Client
            var response = new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                RefreshTokenExpiresIn = (expirationDate - DateTime.UtcNow).TotalSeconds,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role.ToString(),
                ExpiresIn = expiresIn
            };

            return Result<LoginResponse>.Success(response);
        }
    }
}
