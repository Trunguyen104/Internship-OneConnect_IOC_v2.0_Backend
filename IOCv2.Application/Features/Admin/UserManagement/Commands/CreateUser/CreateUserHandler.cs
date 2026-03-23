using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using IOCv2.Application.Common.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using IOCv2.Application.Features.Admin.UserManagement.Common;

namespace IOCv2.Application.Features.Admin.UserManagement.Commands.CreateUser
{
    public class CreateUserHandler : IRequestHandler<CreateUserCommand, Result<CreateUserResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordService _passwordService;
        private readonly ILogger<CreateUserHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly IMapper _mapper;
        private readonly IUserServices _userServices;
        private readonly IBackgroundEmailSender _emailSender;
        private readonly ICacheService _cacheService;

        public CreateUserHandler(
            IUnitOfWork unitOfWork,
            IPasswordService passwordService,
            ILogger<CreateUserHandler> logger,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            IMapper mapper,
            IUserServices userServices,
            IBackgroundEmailSender emailSender,
            ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _passwordService = passwordService;
            _logger = logger;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _mapper = mapper;
            _userServices = userServices;
            _emailSender = emailSender;
            _cacheService = cacheService;
        }

        public async Task<Result<CreateUserResponse>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Creating User {Email} with Role {Role} by Auditor {AuditorId}", 
                request.Email, request.Role, _currentUserService.UserId);

            // 1. Auditor Validation & Scoping
            if (!Guid.TryParse(_currentUserService.UserId, out var auditorId))
            {
                _logger.LogWarning("Invalid Auditor ID: {AuditorId}", _currentUserService.UserId);
                throw new UnauthorizedAccessException(_messageService.GetMessage(MessageKeys.Users.InvalidAuditor));
            }

            var auditorRoleStr = _currentUserService.Role;
            if (!Enum.TryParse<UserRole>(auditorRoleStr, true, out var auditorRole))
            {
                throw new UnauthorizedAccessException(_messageService.GetMessage(MessageKeys.Common.AccessDenied));
            }

            var targetRole = request.Role;
            var auditorUnitId = _currentUserService.UnitId;

            // Hierarchical Scoping Rules
            if (auditorRole == UserRole.SchoolAdmin)
            {
                if (targetRole != UserRole.Student)
                {
                    _logger.LogWarning("Access Denied: SchoolAdmin attempted to create {Role}", targetRole);
                    throw new BusinessException(_messageService.GetMessage(MessageKeys.Common.AccessDenied));
                }

                if (request.UnitId.ToString() != auditorUnitId)
                {
                    _logger.LogWarning("Access Denied: SchoolAdmin attempted to create student in another unit. Requested: {RequestedUnit}, Auditor: {AuditorUnit}", request.UnitId, auditorUnitId);
                    throw new BusinessException(_messageService.GetMessage(MessageKeys.Common.AccessDenied));
                }
            }
            else if (auditorRole == UserRole.EnterpriseAdmin)
            {
                if (targetRole != UserRole.HR && targetRole != UserRole.Mentor)
                {
                    _logger.LogWarning("Access Denied: EnterpriseAdmin attempted to create {Role}", targetRole);
                    throw new BusinessException(_messageService.GetMessage(MessageKeys.Common.AccessDenied));
                }

                if (request.UnitId.ToString() != auditorUnitId)
                {
                    _logger.LogWarning("Access Denied: EnterpriseAdmin attempted to create staff in another unit. Requested: {RequestedUnit}, Auditor: {AuditorUnit}", request.UnitId, auditorUnitId);
                    throw new BusinessException(_messageService.GetMessage(MessageKeys.Common.AccessDenied));
                }
            }
            else if (auditorRole != UserRole.SuperAdmin && auditorRole != UserRole.Moderator)
            {
                _logger.LogWarning("Access Denied: Auditor with role {AuditorRole} attempted to create user", auditorRole);
                throw new BusinessException(_messageService.GetMessage(MessageKeys.Common.AccessDenied));
            }

            // 2. Conflict Checks
            var emailExists = await _unitOfWork.Repository<User>()
                .ExistsAsync(u => u.Email == request.Email, cancellationToken);
            if (emailExists)
            {
                throw new BusinessException(_messageService.GetMessage(MessageKeys.Users.EmailConflict));
            }

            if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
            {
                var phoneExists = await _unitOfWork.Repository<User>()
                    .ExistsAsync(u => u.PhoneNumber == request.PhoneNumber, cancellationToken);
                if (phoneExists)
                {
                    throw new BusinessException(_messageService.GetMessage(MessageKeys.Profile.PhoneExists));
                }
            }

            // 3. Entity Integrity Validation
            if ((targetRole == UserRole.SchoolAdmin || targetRole == UserRole.Student) && request.UnitId.HasValue)
            {
                if (!await _unitOfWork.Repository<University>().ExistsAsync(u => u.UniversityId == request.UnitId.Value, cancellationToken))
                    throw new NotFoundException(_messageService.GetMessage(MessageKeys.University.NotFound));
            }
            else if ((targetRole == UserRole.EnterpriseAdmin || targetRole == UserRole.HR || targetRole == UserRole.Mentor) && request.UnitId.HasValue)
            {
                if (!await _unitOfWork.Repository<Enterprise>().ExistsAsync(e => e.EnterpriseId == request.UnitId.Value, cancellationToken))
                    throw new NotFoundException(_messageService.GetMessage(MessageKeys.Enterprise.NotFound));
            }

            // 4. Persistence
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try {
                var userId = Guid.NewGuid();
                var userCode = await _userServices.GenerateUserCodeAsync(targetRole, cancellationToken);
                var randomPassword = _passwordService.GenerateRandomPassword();
                var passwordHash = _passwordService.HashPassword(randomPassword);

                var user = new User(userId, userCode, request.Email, request.FullName, targetRole, passwordHash);
                user.UpdateProfile(request.FullName, request.PhoneNumber, null, null, null, request.Address);
                user.SetStatus(UserStatus.Active);

                await _unitOfWork.Repository<User>().AddAsync(user, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);

                // Handle Scoped Entities
                if ((targetRole == UserRole.SchoolAdmin || targetRole == UserRole.Student) && request.UnitId.HasValue)
                {
                    await _unitOfWork.Repository<UniversityUser>().AddAsync(new UniversityUser { UserId = user.UserId, UniversityId = request.UnitId.Value }, cancellationToken);

                    if (targetRole == UserRole.Student)
                    {
                        await _unitOfWork.Repository<Student>().AddAsync(new Student { StudentId = Guid.NewGuid(), UserId = user.UserId, InternshipStatus = StudentStatus.NO_INTERNSHIP }, cancellationToken);
                    }
                }
                else if ((targetRole == UserRole.EnterpriseAdmin || targetRole == UserRole.HR || targetRole == UserRole.Mentor) && request.UnitId.HasValue)
                {
                    await _unitOfWork.Repository<EnterpriseUser>().AddAsync(new EnterpriseUser { UserId = user.UserId, EnterpriseId = request.UnitId.Value }, cancellationToken);
                }

                // Auditor Safety Check for Audit Logs (Fix for FK Violation after resets)
                Guid? auditPerformedBy = auditorId;
                var auditorExists = await _unitOfWork.Repository<User>().ExistsAsync(u => u.UserId == auditorId, cancellationToken);
                if (!auditorExists)
                {
                    _logger.LogWarning("Auditor {AuditorId} not found in database. Setting PerformedById to NULL in audit logs.", auditorId);
                    auditPerformedBy = null;
                }

                var auditLog = new AuditLog
                {
                    AuditLogId = Guid.NewGuid(),
                    Action = AuditAction.Create,
                    EntityType = nameof(User),
                    EntityId = user.UserId,
                    PerformedById = auditPerformedBy,
                    Reason = $"Created User: {user.UserCode} | Role: {targetRole}",
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Repository<AuditLog>().AddAsync(auditLog, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);

                // 5. Communications
                await _emailSender.EnqueueAccountCreationEmailAsync(
                    user.Email, user.FullName, user.Email, targetRole.ToString(), randomPassword, 
                    user.UserId, auditPerformedBy, cancellationToken);

                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                
                await _cacheService.RemoveByPatternAsync(UserManagementCacheKeys.UserListPattern(), cancellationToken);

                _logger.LogInformation("Successfully created User {UserCode} (ID: {UserId})", user.UserCode, user.UserId);

                return Result<CreateUserResponse>.Success(_mapper.Map<CreateUserResponse>(user));
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, "Error while creating user {Email}", request.Email);
                throw;
            }
        }
    }
}
