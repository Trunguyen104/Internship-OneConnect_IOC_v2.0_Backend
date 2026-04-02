using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using IOCv2.Application.Features.Admin.UserManagement.Common;

namespace IOCv2.Application.Features.Admin.UserManagement.Commands.DeleteUser
{
    public class DeleteUserHandler : IRequestHandler<DeleteUserCommand, Result<DeleteUserResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<DeleteUserHandler> _logger;

        public DeleteUserHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            ICacheService cacheService,
            ILogger<DeleteUserHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<Result<DeleteUserResponse>> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Deleting User {UserId} by Auditor {AuditorId}", 
                request.UserId, _currentUserService.UserId);

            // 1. Auditor Validation
            if (!Guid.TryParse(_currentUserService.UserId, out var auditorId))
            {
                return Result<DeleteUserResponse>.Failure(_messageService.GetMessage(MessageKeys.Users.InvalidAuditor), ResultErrorType.Unauthorized);
            }

            var auditorRoleStr = _currentUserService.Role;
            if (!Enum.TryParse<UserRole>(auditorRoleStr, true, out var auditorRole))
            {
                return Result<DeleteUserResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.AccessDenied), ResultErrorType.Forbidden);
            }

            var auditorUnitId = _currentUserService.UnitId;

            // 2. Fetch User with Relations for Scoping
            var user = await _unitOfWork.Repository<User>().Query()
                .Include(u => u.UniversityUser)
                .Include(u => u.EnterpriseUser)
                .FirstOrDefaultAsync(u => u.UserId == request.UserId, cancellationToken);

            if (user == null)
            {
                return Result<DeleteUserResponse>.Failure(_messageService.GetMessage(MessageKeys.Users.NotFound), ResultErrorType.NotFound);
            }

            // 3. Hierarchical Scoping Rules for Delete
            if (auditorRole == UserRole.SchoolAdmin)
            {
                if (user.Role != UserRole.Student)
                {
                    _logger.LogWarning("Access Denied: SchoolAdmin attempted to delete non-student {UserId}", user.UserId);
                    return Result<DeleteUserResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.AccessDenied), ResultErrorType.Forbidden);
                }

                if (user.UniversityUser?.UniversityId.ToString() != auditorUnitId)
                {
                    _logger.LogWarning("Access Denied: SchoolAdmin attempted to delete student in another university. User: {UserId}", user.UserId);
                    return Result<DeleteUserResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.AccessDenied), ResultErrorType.Forbidden);
                }
            }
            else if (auditorRole == UserRole.EnterpriseAdmin)
            {
                if (user.Role != UserRole.HR && user.Role != UserRole.Mentor)
                {
                    _logger.LogWarning("Access Denied: EnterpriseAdmin attempted to delete non-staff {UserId}", user.UserId);
                    return Result<DeleteUserResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.AccessDenied), ResultErrorType.Forbidden);
                }

                if (user.EnterpriseUser?.EnterpriseId.ToString() != auditorUnitId)
                {
                    _logger.LogWarning("Access Denied: EnterpriseAdmin attempted to delete staff in another enterprise. User: {UserId}", user.UserId);
                    return Result<DeleteUserResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.AccessDenied), ResultErrorType.Forbidden);
                }
            }
            else if (auditorRole != UserRole.SuperAdmin)
            {
                return Result<DeleteUserResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.AccessDenied), ResultErrorType.Forbidden);
            }

            // 3.1 Self-delete & Last-man-standing protection
            if (request.UserId == auditorId)
            {
                _logger.LogWarning("Self-delete blocked for user {UserId}", request.UserId);
                return Result<DeleteUserResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.Forbidden),
                    ResultErrorType.Forbidden);
            }

            if (user.Role == UserRole.SuperAdmin && user.Status == UserStatus.Active)
            {
                var otherActiveSuperAdminsCount = await _unitOfWork.Repository<User>()
                    .Query()
                    .CountAsync(
                        u => u.Role == UserRole.SuperAdmin
                             && u.Status == UserStatus.Active
                             && u.UserId != user.UserId,
                        cancellationToken);

                if (otherActiveSuperAdminsCount == 0)
                {
                    return Result<DeleteUserResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.Common.Forbidden),
                        ResultErrorType.Forbidden);
                }
            }

            // 4. Delete Execution
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                // BR-USR-DL-04 & BR-USR-DL-05: revoke sessions + free unique constraints after soft delete
                var now = DateTime.UtcNow;
                var suffix = $"_deleted_{now:yyyyMMddHHmmssfff}";

                // Update email/phone before soft-delete so unique indexes won't block future re-use
                user.UpdateEmail($"{user.Email}{suffix}");
                if (!string.IsNullOrWhiteSpace(user.PhoneNumber))
                {
                    var newPhone = $"{user.PhoneNumber}{suffix}";
                    user.UpdateProfile(
                        user.FullName,
                        newPhone,
                        user.AvatarUrl,
                        user.Gender,
                        user.DateOfBirth,
                        user.Address);
                }

                var activeTokens = await _unitOfWork.Repository<RefreshToken>()
                    .Query()
                    .Where(rt => rt.UserId == user.UserId && !rt.IsRevoked)
                    .ToListAsync(cancellationToken);

                foreach (var token in activeTokens)
                {
                    token.IsRevoked = true;
                    token.UpdatedAt = now;
                    await _unitOfWork.Repository<RefreshToken>().UpdateAsync(token, cancellationToken);
                }

                await _unitOfWork.Repository<User>().DeleteAsync(user, cancellationToken);

                var auditLog = new AuditLog
                {
                    AuditLogId = Guid.NewGuid(),
                    Action = AuditAction.Delete,
                    EntityType = nameof(User),
                    EntityId = user.UserId,
                    PerformedById = auditorId,
                    Reason = $"Deleted user {user.FullName} ({user.Email})",
                    CreatedAt = DateTime.UtcNow,
                };
                await _unitOfWork.Repository<AuditLog>().AddAsync(auditLog, cancellationToken);

                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                await _cacheService.RemoveByPatternAsync(UserManagementCacheKeys.UserListPattern(), cancellationToken);
                await _cacheService.RemoveAsync(UserManagementCacheKeys.User(user.UserId), cancellationToken);

                _logger.LogInformation("Successfully deleted User {UserCode} (ID: {UserId})", user.UserCode, user.UserId);

                return Result<DeleteUserResponse>.Success(_mapper.Map<DeleteUserResponse>(user));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete User {UserId}", request.UserId);
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }
    }
}
