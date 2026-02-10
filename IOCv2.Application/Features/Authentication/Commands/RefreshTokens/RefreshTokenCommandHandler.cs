using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Authentication.Commands.Login;
using IOCv2.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.Authentication.Commands.RefreshTokens
{
    public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<LoginResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITokenService _tokenService;
        private readonly IMessageService _messageService;

        public RefreshTokenCommandHandler(IUnitOfWork unitOfWork, ITokenService tokenService, IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _tokenService = tokenService;
            _messageService = messageService;
        }

        public async Task<Result<LoginResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            var storedRefreshToken = await _unitOfWork.Repository<Domain.Entities.RefreshToken>()
                .Query()
                .Include(x => x.User) // Load user explicitly
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, cancellationToken);

            // Validation Checks
            if (storedRefreshToken == null)
            {
                return Result<LoginResponse>.Failure(_messageService.GetMessage(MessageKeys.Auth.RefreshTokenNotFound));
            }

            if (storedRefreshToken.Expires < DateTime.UtcNow)
            {
                return Result<LoginResponse>.Failure(_messageService.GetMessage(MessageKeys.Auth.RefreshTokenExpired));
            }

            if (storedRefreshToken.IsRevoked)
            {
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
                UserId = user.Id,
            };

            await _unitOfWork.Repository<Domain.Entities.RefreshToken>().AddAsync(newRefreshTokenEntity);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            var response = new LoginResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                Email = user.Email,
                Role = user.Role.ToString(),
                RefreshTokenExpiresIn = (newRefreshTokenEntity.Expires - DateTime.UtcNow).TotalSeconds,
                ExpiresIn = _tokenService.GetTokenExpirationSeconds()
            };

            return Result<LoginResponse>.Success(response);
        }
    }
}
