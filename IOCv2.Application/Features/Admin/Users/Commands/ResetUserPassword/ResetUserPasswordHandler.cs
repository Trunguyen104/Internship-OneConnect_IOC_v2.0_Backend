using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.Admin.Users.Commands.ResetUserPassword
{
    public class ResetUserPasswordHandler : IRequestHandler<ResetUserPasswordCommand, Result<ResetUserPasswordResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordService _passwordService;
        private readonly IBackgroundEmailSender _emailSender;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;

        public ResetUserPasswordHandler(
            IUnitOfWork unitOfWork,
            IPasswordService passwordService,
            IBackgroundEmailSender emailSender,
            ICurrentUserService currentUserService,
            IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _passwordService = passwordService;
            _emailSender = emailSender;
            _currentUserService = currentUserService;
            _messageService = messageService;
        }

        public async Task<Result<ResetUserPasswordResponse>> Handle(ResetUserPasswordCommand request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var auditorId))
            {
                return Result<ResetUserPasswordResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Users.InvalidAuditor),
                    ResultErrorType.Unauthorized
                );
            }

            var user = await _unitOfWork.Repository<User>()
                .GetByIdAsync(request.UserId, cancellationToken);

            if (user == null)
                return Result<ResetUserPasswordResponse>.Failure(_messageService.GetMessage(MessageKeys.Users.NotFound));

            if (user.Status != UserStatus.Active)
                return Result<ResetUserPasswordResponse>.Failure(_messageService.GetMessage(MessageKeys.Users.NotActive));

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var newPassword = string.IsNullOrEmpty(request.NewPassword)
                   ? _passwordService.GenerateRandomPassword()
                   : request.NewPassword;

                user.PasswordHash = _passwordService.HashPassword(newPassword);
                await _unitOfWork.Repository<User>().UpdateAsync(user, cancellationToken);

                // Revoke refresh tokens
                var refreshTokens = await _unitOfWork.Repository<RefreshToken>()
                  .Query()
                  .Where(rt => rt.UserId == user.UserId && !rt.IsRevoked)
                  .ToListAsync(cancellationToken);

                foreach (var token in refreshTokens)
                {
                    token.IsRevoked = true;
                    token.UpdatedAt = DateTime.UtcNow;
                }

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
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }
    }
}
