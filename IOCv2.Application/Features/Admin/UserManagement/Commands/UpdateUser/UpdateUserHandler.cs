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

namespace IOCv2.Application.Features.Admin.UserManagement.Commands.UpdateUser
{
    public class UpdateUserHandler : IRequestHandler<UpdateUserCommand, Result<UpdateUserResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<UpdateUserHandler> _logger;

        public UpdateUserHandler(
            IUnitOfWork unitOfWork, 
            IMapper mapper, 
            ICurrentUserService currentUserService, 
            IMessageService messageService, 
            ICacheService cacheService,
            ILogger<UpdateUserHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<Result<UpdateUserResponse>> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Updating User {UserId} by Auditor {AuditorId}", 
                request.UserId, _currentUserService.UserId);

            // 1. Auditor Validation
            if (!Guid.TryParse(_currentUserService.UserId, out var auditorId))
            {
                return Result<UpdateUserResponse>.Failure(_messageService.GetMessage(MessageKeys.Users.InvalidAuditor), ResultErrorType.Unauthorized);
            }

            var auditorRoleStr = _currentUserService.Role;
            if (!Enum.TryParse<UserRole>(auditorRoleStr, true, out var auditorRole))
            {
                return Result<UpdateUserResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.AccessDenied), ResultErrorType.Forbidden);
            }

            var auditorUnitId = _currentUserService.UnitId;

            // 2. Fetch User with Relations for Scoping
            var userQuery = _unitOfWork.Repository<User>().Query()
                .Include(u => u.Student)
                .Include(u => u.UniversityUser)
                .Include(u => u.EnterpriseUser);

            var user = await userQuery.FirstOrDefaultAsync(u => u.UserId == request.UserId, cancellationToken);

            if (user == null)
            {
                return Result<UpdateUserResponse>.Failure(_messageService.GetMessage(MessageKeys.Users.NotFound), ResultErrorType.NotFound);
            }

            // 3. Hierarchical Scoping Rules for Update
            if (auditorRole == UserRole.SchoolAdmin)
            {
                if (user.Role != UserRole.Student)
                {
                    _logger.LogWarning("Access Denied: SchoolAdmin attempted to update non-student {UserId}", user.UserId);
                    return Result<UpdateUserResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.AccessDenied), ResultErrorType.Forbidden);
                }

                if (user.UniversityUser?.UniversityId.ToString() != auditorUnitId)
                {
                    _logger.LogWarning("Access Denied: SchoolAdmin attempted to update student in another university. User: {UserId}", user.UserId);
                    return Result<UpdateUserResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.AccessDenied), ResultErrorType.Forbidden);
                }
            }
            else if (auditorRole == UserRole.EnterpriseAdmin)
            {
                if (user.Role != UserRole.HR && user.Role != UserRole.Mentor)
                {
                    _logger.LogWarning("Access Denied: EnterpriseAdmin attempted to update non-staff {UserId}", user.UserId);
                    return Result<UpdateUserResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.AccessDenied), ResultErrorType.Forbidden);
                }

                if (user.EnterpriseUser?.EnterpriseId.ToString() != auditorUnitId)
                {
                    _logger.LogWarning("Access Denied: EnterpriseAdmin attempted to update staff in another enterprise. User: {UserId}", user.UserId);
                    return Result<UpdateUserResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.AccessDenied), ResultErrorType.Forbidden);
                }
            }
            else if (auditorRole != UserRole.SuperAdmin && auditorRole != UserRole.Moderator)
            {
                return Result<UpdateUserResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.AccessDenied), ResultErrorType.Forbidden);
            }

            // 4. Update Execution
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                DateOnly? parsedDob = null;
                if (!string.IsNullOrWhiteSpace(request.DateOfBirth) && DateOnly.TryParse(request.DateOfBirth, out var dobVal))
                {
                    parsedDob = dobVal;
                }

                user.UpdateProfile(request.FullName, request.PhoneNumber, request.AvatarUrl, request.Gender, parsedDob);

                if (request.Status.HasValue)
                {
                    user.SetStatus(request.Status.Value);
                }

                if (user.Role == UserRole.Student && user.Student != null)
                {
                    if (request.StudentClass != null) user.Student.ClassName = request.StudentClass;
                    if (request.StudentMajor != null) user.Student.Major = request.StudentMajor;
                    if (request.StudentGpa != null) user.Student.Gpa = request.StudentGpa;
                    await _unitOfWork.Repository<Student>().UpdateAsync(user.Student, cancellationToken);
                }

                await _unitOfWork.Repository<User>().UpdateAsync(user, cancellationToken);

                var auditLog = new AuditLog
                {
                    AuditLogId = Guid.NewGuid(),
                    Action = AuditAction.Update,
                    EntityType = nameof(User),
                    EntityId = user.UserId,
                    PerformedById = auditorId,
                    Reason = $"Updated user {user.UserCode}",
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Repository<AuditLog>().AddAsync(auditLog, cancellationToken);

                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                await _cacheService.RemoveAsync(UserManagementCacheKeys.User(user.UserId), cancellationToken);
                await _cacheService.RemoveByPatternAsync(UserManagementCacheKeys.UserListPattern(), cancellationToken);

                _logger.LogInformation("Successfully updated User {UserCode} (ID: {UserId})", user.UserCode, user.UserId);

                return Result<UpdateUserResponse>.Success(_mapper.Map<UpdateUserResponse>(user));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update User {UserId}", request.UserId);
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }
    }
}
