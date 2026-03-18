using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using IOCv2.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Authentication.Commands.ChangePassword
{
    public class ChangePasswordHandler : IRequestHandler<ChangePasswordCommand, Result<string>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordService _passwordService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IRateLimiter _rateLimiter;
        private readonly IMessageService _messageService;
        private readonly ILogger<ChangePasswordHandler> _logger;

        public ChangePasswordHandler(
            IUnitOfWork unitOfWork,
            IPasswordService passwordService,
            ICurrentUserService currentUserService,
            IRateLimiter rateLimiter,
            IMessageService messageService,
            ILogger<ChangePasswordHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _passwordService = passwordService;
            _currentUserService = currentUserService;
            _rateLimiter = rateLimiter;
            _messageService = messageService;
            _logger = logger;
        }

        public async Task<Result<string>> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("ChangePassword attempt without valid user ID");
                return Result<string>.Failure(_messageService.GetMessage(MessageKeys.Auth.UserNotLoggedIn));
            }

            var user = await _unitOfWork.Repository<User>()
                .Query()
                .FirstOrDefaultAsync(u => u.UserId.ToString() == userId, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("ChangePassword attempt for non-existent user ID {UserId}", userId);
                return Result<string>.Failure(_messageService.GetMessage(MessageKeys.Auth.InvalidAction));
            }

            if (user.Status != UserStatus.Active)
            {
                _logger.LogWarning("ChangePassword attempt for inactive user ID {UserId}", userId);
                return Result<string>.Failure(_messageService.GetMessage(MessageKeys.Auth.InvalidAction));
            }

            var key = $"cp:{userId}";

            // Check if user is blocked
            if (await _rateLimiter.IsBlockedAsync(key, cancellationToken))
                return Result<string>.Failure(_messageService.GetMessage(MessageKeys.Auth.TooManyAttempts));

            // Verify current password
            if (!_passwordService.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            {
                await RegisterFailedAttempt(key, cancellationToken);
                return Result<string>.Failure(_messageService.GetMessage(MessageKeys.Password.IncorrectCurrent));
            }

            // All validations passed - reset fail count
            await _rateLimiter.ResetAsync(key, cancellationToken);

            try
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

            // Update password using rich domain method
            user.UpdatePassword(_passwordService.HashPassword(request.NewPassword));

            //Revoke existing refresh tokens
            var refreshTokens = await _unitOfWork.Repository<Domain.Entities.RefreshToken>()
                .Query()
                .Where(rt => rt.UserId == user.UserId && !rt.IsRevoked)
                .ToListAsync(cancellationToken);

            foreach (var token in refreshTokens)
            {
                token.IsRevoked = true;
                token.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.Repository<Domain.Entities.RefreshToken>().UpdateAsync(token, cancellationToken);
            }

            //Log password reset
            await _unitOfWork.Repository<AuditLog>().AddAsync(new AuditLog
            {
                AuditLogId = Guid.NewGuid(),
                Action = AuditAction.Update,
                EntityType = nameof(User),
                EntityId = user.UserId,
                PerformedById = user.UserId, // self-change
                Reason = "SelfChange",
            });

            await _unitOfWork.SaveChangeAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Successfully changed password for user ID {UserId}", userId);

            return Result<string>.Success(_messageService.GetMessage(MessageKeys.Auth.PasswordChangedSuccess));
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, "Error changing password for user ID {UserId}", userId);
                return Result<string>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError));
            }
        }

        private async Task RegisterFailedAttempt(string key, CancellationToken cancellationToken)
        {
            await _rateLimiter.RegisterFailAsync(
                key,
                limit: 5,
                window: TimeSpan.FromMinutes(15),
                blockFor: TimeSpan.FromMinutes(15),
                cancellationToken);
        }
    }
}
