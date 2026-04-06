using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using IOCv2.Application.Features.Admin.UserManagement.Common;

namespace IOCv2.Application.Features.Admin.UserManagement.Commands.ResetUserPassword
{
    public class ResetUserPasswordHandler : IRequestHandler<ResetUserPasswordCommand, Result<ResetUserPasswordResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordService _passwordService;
        private readonly IBackgroundEmailSender _emailSender;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<ResetUserPasswordHandler> _logger;

        public ResetUserPasswordHandler(
            IUnitOfWork unitOfWork,
            IPasswordService passwordService,
            IBackgroundEmailSender emailSender,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            ICacheService cacheService,
            ILogger<ResetUserPasswordHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _passwordService = passwordService;
            _emailSender = emailSender;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<Result<ResetUserPasswordResponse>> Handle(ResetUserPasswordCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Resetting password for User {UserId} by {AuditorId}", 
                request.UserId, _currentUserService.UserId);

            // 1. Auditor Validation
            if (!Guid.TryParse(_currentUserService.UserId, out var auditorId))
            {
                return Result<ResetUserPasswordResponse>.Failure(_messageService.GetMessage(MessageKeys.Users.InvalidAuditor), ResultErrorType.Unauthorized);
            }

            var auditorRoleStr = _currentUserService.Role;
            if (!Enum.TryParse<UserRole>(auditorRoleStr, true, out var auditorRole))
            {
                return Result<ResetUserPasswordResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.AccessDenied), ResultErrorType.Forbidden);
            }

            var auditorUnitId = _currentUserService.UnitId;

            // 2. Fetch User with Relations for Scoping
            var user = await _unitOfWork.Repository<User>().Query()
                .Include(u => u.UniversityUser)
                .Include(u => u.EnterpriseUser)
                .FirstOrDefaultAsync(u => u.UserId == request.UserId, cancellationToken);

            if (user == null)
            {
                return Result<ResetUserPasswordResponse>.Failure(_messageService.GetMessage(MessageKeys.Users.NotFound), ResultErrorType.NotFound);
            }

            // 3. Hierarchical Scoping Rules
            if (auditorRole == UserRole.SchoolAdmin)
            {
                if (user.Role != UserRole.Student || user.UniversityUser?.UniversityId.ToString() != auditorUnitId)
                {
                    _logger.LogWarning("Access Denied: SchoolAdmin attempted to reset password for user {UserId}", user.UserId);
                    return Result<ResetUserPasswordResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.AccessDenied), ResultErrorType.Forbidden);
                }
            }
            else if (auditorRole == UserRole.EnterpriseAdmin)
            {
                if ((user.Role != UserRole.HR && user.Role != UserRole.Mentor) || user.EnterpriseUser?.EnterpriseId.ToString() != auditorUnitId)
                {
                    _logger.LogWarning("Access Denied: EnterpriseAdmin attempted to reset password for user {UserId}", user.UserId);
                    return Result<ResetUserPasswordResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.AccessDenied), ResultErrorType.Forbidden);
                }
            }
            else if (auditorRole != UserRole.SuperAdmin)
            {
                return Result<ResetUserPasswordResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.AccessDenied), ResultErrorType.Forbidden);
            }

            if (user.Status != UserStatus.Active)
            {
                return Result<ResetUserPasswordResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Users.NotActive),
                    ResultErrorType.Forbidden);
            }

            // 4. Execution
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var newPassword = string.IsNullOrEmpty(request.NewPassword)
                   ? _passwordService.GenerateRandomPassword()
                   : request.NewPassword;

                var passwordHash = _passwordService.HashPassword(newPassword);
                user.UpdatePassword(passwordHash);

                await _unitOfWork.Repository<User>().UpdateAsync(user, cancellationToken);

                // Revoke refresh tokens
                var activeTokens = await _unitOfWork.Repository<RefreshToken>()
                    .Query()
                    .Where(rt => rt.UserId == user.UserId && !rt.IsRevoked)
                    .ToListAsync(cancellationToken);

                foreach (var token in activeTokens)
                {
                    token.IsRevoked = true;
                    token.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.Repository<RefreshToken>().UpdateAsync(token, cancellationToken);
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

                // Send email
                await _emailSender.EnqueuePasswordResetBySuperAdminEmailAsync(
                    user.Email,
                    user.FullName,
                    newPassword,
                    auditorRole.ToString(),
                    user.UserId,
                    auditorId,
                    cancellationToken);

                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                await _cacheService.RemoveByPatternAsync(UserManagementCacheKeys.UserListPattern(), cancellationToken);
                await _cacheService.RemoveAsync(UserManagementCacheKeys.User(user.UserId), cancellationToken);

                _logger.LogInformation("Successfully reset password for User {UserCode} (ID: {UserId})", user.UserCode, user.UserId);

                return Result<ResetUserPasswordResponse>.Success(new ResetUserPasswordResponse
                {
                    UserId = user.UserId,
                    UserCode = user.UserCode,
                    FullName = user.FullName,
                    Email = user.Email,
                    ResetAt = DateTime.UtcNow,
                    Message = _messageService.GetMessage(MessageKeys.ResetPassword.SuccessWithEmail, user.Email)
                });
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
