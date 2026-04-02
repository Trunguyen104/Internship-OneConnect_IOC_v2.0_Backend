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

namespace IOCv2.Application.Features.Admin.UserManagement.Commands.ToggleUserStatus
{
    public class ToggleUserStatusHandler : IRequestHandler<ToggleUserStatusCommand, Result<ToggleUserStatusResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<ToggleUserStatusHandler> _logger;

        public ToggleUserStatusHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            ICacheService cacheService,
            ILogger<ToggleUserStatusHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<Result<ToggleUserStatusResponse>> Handle(ToggleUserStatusCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Toggling status for User {UserId} to {NewStatus} by {AuditorId}", 
                request.UserId, request.NewStatus, _currentUserService.UserId);

            // 1. Auditor Validation
            if (!Guid.TryParse(_currentUserService.UserId, out var auditorId))
            {
                return Result<ToggleUserStatusResponse>.Failure(_messageService.GetMessage(MessageKeys.Users.InvalidAuditor), ResultErrorType.Unauthorized);
            }

            var auditorRoleStr = _currentUserService.Role;
            if (!Enum.TryParse<UserRole>(auditorRoleStr, true, out var auditorRole))
            {
                return Result<ToggleUserStatusResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.AccessDenied), ResultErrorType.Forbidden);
            }

            var auditorUnitId = _currentUserService.UnitId;

            // 2. Fetch User with Relations for Scoping
            var user = await _unitOfWork.Repository<User>().Query()
                .Include(u => u.UniversityUser)
                .Include(u => u.EnterpriseUser)
                .FirstOrDefaultAsync(u => u.UserId == request.UserId, cancellationToken);

            if (user == null)
            {
                return Result<ToggleUserStatusResponse>.Failure(_messageService.GetMessage(MessageKeys.Users.NotFound), ResultErrorType.NotFound);
            }

            // 3. Hierarchical Scoping Rules
            if (auditorRole == UserRole.SchoolAdmin)
            {
                if (user.Role != UserRole.Student || user.UniversityUser?.UniversityId.ToString() != auditorUnitId)
                {
                    _logger.LogWarning("Access Denied: SchoolAdmin attempted to toggle status for user {UserId}", user.UserId);
                    return Result<ToggleUserStatusResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.AccessDenied), ResultErrorType.Forbidden);
                }
            }
            else if (auditorRole == UserRole.EnterpriseAdmin)
            {
                if ((user.Role != UserRole.HR && user.Role != UserRole.Mentor) || user.EnterpriseUser?.EnterpriseId.ToString() != auditorUnitId)
                {
                    _logger.LogWarning("Access Denied: EnterpriseAdmin attempted to toggle status for user {UserId}", user.UserId);
                    return Result<ToggleUserStatusResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.AccessDenied), ResultErrorType.Forbidden);
                }
            }
            else if (auditorRole != UserRole.SuperAdmin && auditorRole != UserRole.Moderator)
            {
                return Result<ToggleUserStatusResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.AccessDenied), ResultErrorType.Forbidden);
            }

            // 4. Execution
            user.SetStatus(request.NewStatus);

            try
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);
                
                await _unitOfWork.Repository<User>().UpdateAsync(user, cancellationToken);

                var auditLog = new AuditLog
                {
                    AuditLogId = Guid.NewGuid(),
                    Action = AuditAction.Update,
                    EntityType = nameof(User),
                    EntityId = user.UserId,
                    PerformedById = auditorId,
                    Reason = $"Toggled user {user.UserCode} status to {request.NewStatus}",
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Repository<AuditLog>().AddAsync(auditLog, cancellationToken);

                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                await _cacheService.RemoveByPatternAsync(UserManagementCacheKeys.UserListPattern(), cancellationToken);
                await _cacheService.RemoveAsync(UserManagementCacheKeys.User(user.UserId), cancellationToken);

                _logger.LogInformation("Successfully toggled status for User {UserCode} (ID: {UserId})", user.UserCode, user.UserId);

                return Result<ToggleUserStatusResponse>.Success(_mapper.Map<ToggleUserStatusResponse>(user));
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, "Failed to toggle status for User {UserId}", request.UserId);
                throw;
            }
        }
    }
}
