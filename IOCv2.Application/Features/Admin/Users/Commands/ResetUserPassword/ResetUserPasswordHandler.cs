using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Admin.Users.Commands.ResetUserPassword
{
    public class ResetUserPasswordHandler : IRequestHandler<ResetUserPasswordCommand, Result<ResetUserPasswordResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordService _passwordService;
        private readonly IBackgroundEmailSender _emailSender;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ILogger<ResetUserPasswordHandler> _logger;

        public ResetUserPasswordHandler(
            IUnitOfWork unitOfWork,
            IPasswordService passwordService,
            IBackgroundEmailSender emailSender,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            ILogger<ResetUserPasswordHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _passwordService = passwordService;
            _emailSender = emailSender;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _logger = logger;
        }

        public async Task<Result<ResetUserPasswordResponse>> Handle(ResetUserPasswordCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Resetting password for User {UserId} by Auditor {AuditorId}", 
                request.UserId, _currentUserService.UserId);

            if (!Guid.TryParse(_currentUserService.UserId, out var auditorId))
            {
                _logger.LogWarning("Invalid Auditor ID: {AuditorId}", _currentUserService.UserId);
                return Result<ResetUserPasswordResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Users.InvalidAuditor),
                    ResultErrorType.Unauthorized
                );
            }

            var user = await _unitOfWork.Repository<User>()
                .GetByIdAsync(request.UserId, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found for password reset", request.UserId);
                return Result<ResetUserPasswordResponse>.Failure(_messageService.GetMessage(MessageKeys.Users.NotFound));
            }

            if (user.Status != UserStatus.Active)
            {
                _logger.LogInformation("Attempted to reset password for inactive user {UserId}", request.UserId);
                return Result<ResetUserPasswordResponse>.Failure(_messageService.GetMessage(MessageKeys.Users.NotActive));
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var newPassword = string.IsNullOrEmpty(request.NewPassword)
                   ? _passwordService.GenerateRandomPassword()
                   : request.NewPassword;

                var passwordHash = _passwordService.HashPassword(newPassword);
                
                // Use rich domain method
                user.UpdatePassword(passwordHash);

                await _unitOfWork.Repository<User>().UpdateAsync(user, cancellationToken);

                // Revoke refresh tokens (Bulk update)
                await _unitOfWork.Repository<RefreshToken>()
                    .Query()
                    .Where(rt => rt.UserId == user.UserId && !rt.IsRevoked)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(rt => rt.IsRevoked, true)
                        .SetProperty(rt => rt.UpdatedAt, DateTime.UtcNow), 
                        cancellationToken);

                var auditLog = new AuditLog
                {
                    AuditLogId = Guid.NewGuid(),
                    Action = AuditAction.ResetPassword,
                    EntityType = nameof(User),
                    EntityId = user.UserId,
                    PerformedById = auditorId,
                    Reason = request.Reason,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Repository<AuditLog>().AddAsync(auditLog, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);

                await _emailSender.EnqueuePasswordResetBySuperAdminEmailAsync(
                    user.Email,
                    user.FullName,
                    newPassword,
                    "superAdmin",
                    user.UserId,
                    auditorId,
                    cancellationToken);

                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation("Successfully reset password for User {UserCode} (ID: {UserId})", user.UserCode, user.UserId);

                var response = new ResetUserPasswordResponse
                {
                    UserId = user.UserId,
                    UserCode = user.UserCode,
                    FullName = user.FullName,
                    Email = user.Email,
                    ResetAt = DateTime.UtcNow,
                    Message = _messageService.GetMessage(MessageKeys.ResetPassword.SuccessWithEmail, user.Email)
                };
                return Result<ResetUserPasswordResponse>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reset password for user {UserId}", request.UserId);
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }
    }
}
