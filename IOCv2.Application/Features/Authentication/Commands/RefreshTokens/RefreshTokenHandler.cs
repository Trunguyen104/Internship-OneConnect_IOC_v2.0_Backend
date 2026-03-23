using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Authentication.Commands.Login;
using IOCv2.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Authentication.Commands.RefreshTokens
{
    public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, Result<LoginResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITokenService _tokenService;
        private readonly IMessageService _messageService;
        private readonly ILogger<RefreshTokenHandler> _logger;

        public RefreshTokenHandler(IUnitOfWork unitOfWork, ITokenService tokenService, IMessageService messageService, ILogger<RefreshTokenHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _tokenService = tokenService;
            _messageService = messageService;
            _logger = logger;
        }

        public async Task<Result<LoginResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Refreshing access token");

            var storedRefreshToken = await _unitOfWork.Repository<Domain.Entities.RefreshToken>()
                .Query()
                .Include(x => x.User) // Load user explicitly
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, cancellationToken);

            // Validation Checks
            if (storedRefreshToken == null)
            {
                _logger.LogWarning("Refresh token not found");
                return Result<LoginResponse>.Failure(_messageService.GetMessage(MessageKeys.Auth.RefreshTokenNotFound));
            }

            if (storedRefreshToken.Expires < DateTime.UtcNow)
            {
                _logger.LogWarning("Refresh token expired for user {UserId}", storedRefreshToken.UserId);
                return Result<LoginResponse>.Failure(_messageService.GetMessage(MessageKeys.Auth.RefreshTokenExpired));
            }

            if (storedRefreshToken.IsRevoked)
            {
                _logger.LogWarning("Refresh token revoked for user {UserId}", storedRefreshToken.UserId);
                return Result<LoginResponse>.Failure(_messageService.GetMessage(MessageKeys.Auth.RefreshTokenRevoked));
            }

            // Reevoke current token (Token Rotation)
            storedRefreshToken.IsRevoked = true;
            storedRefreshToken.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Repository<Domain.Entities.RefreshToken>().UpdateAsync(storedRefreshToken, cancellationToken);

            // Generate new tokens
            var user = storedRefreshToken.User;

            // Check if user is still active
            if (user.Status != Domain.Enums.UserStatus.Active)
            {
                _logger.LogWarning("Refresh rejected because account inactive for user {UserId}", user.UserId);
                return Result<LoginResponse>.Failure(_messageService.GetMessage(MessageKeys.Auth.AccountInactive));
            }

            var newAccessToken = _tokenService.GenerateAccessToken(user);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            // Logic: Inherit "Remember Me" status from the old token
            // Check if the old token had a duration significantly longer than the default (7 days)
            var defaultDays = _tokenService.GetRefreshTokenExpirationDays();
            var oldDurationDays = (storedRefreshToken.Expires - storedRefreshToken.CreatedAt).TotalDays;

            // If old usage was > Default + 1 (allow small margin), treat as RememberMe (30 days)
            var isLongLived = oldDurationDays > defaultDays + 1;
            var newDurationDays = isLongLived ? 30 : defaultDays;

            var newRefreshTokenEntity = new Domain.Entities.RefreshToken
            {
                Token = newRefreshToken,
                Expires = DateTime.UtcNow.AddDays(newDurationDays),
                UserId = user.UserId,
            };

            await _unitOfWork.Repository<Domain.Entities.RefreshToken>().AddAsync(newRefreshTokenEntity);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            var response = new LoginResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                Email = user.Email,
                Role = user.Role,
                RefreshTokenExpiresIn = (newRefreshTokenEntity.Expires - DateTime.UtcNow).TotalSeconds,
                ExpiresIn = _tokenService.GetTokenExpirationSeconds()
            };

            _logger.LogInformation("Successfully refreshed token for user {UserId}", user.UserId);

            return Result<LoginResponse>.Success(response);
        }
    }
}
