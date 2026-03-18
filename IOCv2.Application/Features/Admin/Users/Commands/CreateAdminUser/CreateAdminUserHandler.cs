using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using IOCv2.Application.Common.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Admin.Users.Commands.CreateAdminUser
{
    public class CreateAdminUserHandler : IRequestHandler<CreateAdminUserCommand, Result<CreateAdminUserResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordService _passwordService;
        private readonly ILogger<CreateAdminUserHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly IMapper _mapper;
        private readonly IUserServices _userServices;
        private readonly IBackgroundEmailSender _emailSender;
        private readonly ICacheService _cacheService;

        public CreateAdminUserHandler(
            IUnitOfWork unitOfWork,
            IPasswordService passwordService,
            ILogger<CreateAdminUserHandler> logger,
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

        public async Task<Result<CreateAdminUserResponse>> Handle(CreateAdminUserCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Creating Admin User {Email} with Role {Role} by Auditor {AuditorId}", 
                request.Email, request.Role, _currentUserService.UserId);

            if (!Guid.TryParse(_currentUserService.UserId, out var auditorId))
            {
                _logger.LogWarning("Invalid Auditor ID: {AuditorId}", _currentUserService.UserId);
                throw new UnauthorizedAccessException(_messageService.GetMessage(MessageKeys.Users.InvalidAuditor));
            }

            var parsedRole = request.Role;

            // Business Rules for delegated management
            var auditorRoleStr = _currentUserService.Role;
            if (Enum.TryParse<UserRole>(auditorRoleStr, true, out var auditorRole))
            {
                if (auditorRole == UserRole.SchoolAdmin)
                {
                    if (parsedRole != UserRole.Student)
                    {
                        _logger.LogWarning("Access Denied: SchoolAdmin attempted to create {Role}", parsedRole);
                        throw new BusinessException(_messageService.GetMessage(MessageKeys.Common.AccessDenied));
                    }
                }
                else if (auditorRole == UserRole.EnterpriseAdmin)
                {
                    if (parsedRole != UserRole.HR && parsedRole != UserRole.Mentor)
                    {
                        _logger.LogWarning("Access Denied: EnterpriseAdmin attempted to create {Role}", parsedRole);
                        throw new BusinessException(_messageService.GetMessage(MessageKeys.Common.AccessDenied));
                    }
                }
                else if (auditorRole != UserRole.SuperAdmin && auditorRole != UserRole.Moderator)
                {
                    _logger.LogWarning("Access Denied: Auditor with role {AuditorRole} attempted to create user", auditorRole);
                    throw new BusinessException(_messageService.GetMessage(MessageKeys.Common.AccessDenied));
                }
            }

            var emailExists = await _unitOfWork.Repository<User>()
                .ExistsAsync(u => u.Email == request.Email, cancellationToken);
            if (emailExists)
            {
                _logger.LogWarning("Email Conflict: {Email} already exists", request.Email);
                throw new BusinessException(_messageService.GetMessage(MessageKeys.Users.EmailConflict));
            }

            // Validate unit exists for role that requires it
            if ((parsedRole == UserRole.SchoolAdmin || parsedRole == UserRole.Student) && request.UnitId.HasValue)
            {
                var universityExists = await _unitOfWork.Repository<University>()
                    .ExistsAsync(u => u.UniversityId == request.UnitId.Value, cancellationToken);
                if (!universityExists)
                {
                    _logger.LogWarning("University Not Found: {UnitId}", request.UnitId);
                    throw new NotFoundException(_messageService.GetMessage(MessageKeys.University.NotFound));
                }
            }
            else if ((parsedRole == UserRole.EnterpriseAdmin || parsedRole == UserRole.HR || parsedRole == UserRole.Mentor) && request.UnitId.HasValue)
            {
                var enterpriseExists = await _unitOfWork.Repository<Enterprise>()
                    .ExistsAsync(e => e.EnterpriseId == request.UnitId.Value, cancellationToken);
                if (!enterpriseExists)
                {
                    _logger.LogWarning("Enterprise Not Found: {UnitId}", request.UnitId);
                    throw new NotFoundException(_messageService.GetMessage(MessageKeys.Enterprise.NotFound));
                }
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try {
                // Prepare domain data
                var userId = Guid.NewGuid();
                var userCode = await _userServices.GenerateUserCodeAsync(parsedRole, cancellationToken);
                var randomPassword = _passwordService.GenerateRandomPassword();
                var passwordHash = _passwordService.HashPassword(randomPassword);

                // Create user using rich domain constructor
                var user = new User(
                    userId,
                    userCode,
                    request.Email,
                    request.FullName,
                    parsedRole,
                    passwordHash
                );

                // Update non-constructor fields if provided
                user.UpdateProfile(
                    request.FullName,
                    request.PhoneNumber,
                    request.AvatarUrl,
                    null,
                    null
                );

                await _unitOfWork.Repository<User>().AddAsync(user, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);

                // Create unit linkage
                if ((parsedRole == UserRole.SchoolAdmin || parsedRole == UserRole.Student) && request.UnitId.HasValue)
                {
                    var universityUser = new UniversityUser
                    {
                        UserId = user.UserId,
                        UniversityId = request.UnitId.Value
                    };
                    await _unitOfWork.Repository<UniversityUser>().AddAsync(universityUser, cancellationToken);

                    if (parsedRole == UserRole.Student)
                    {
                        var student = new Student
                        {
                            StudentId = Guid.NewGuid(),
                            UserId = user.UserId,
                            InternshipStatus = StudentStatus.NO_INTERNSHIP
                        };
                        await _unitOfWork.Repository<Student>().AddAsync(student, cancellationToken);
                    }
                }
                else if ((parsedRole == UserRole.EnterpriseAdmin || parsedRole == UserRole.HR || parsedRole == UserRole.Mentor) && request.UnitId.HasValue)
                {
                    var enterpriseUser = new EnterpriseUser
                    {
                        UserId = user.UserId,
                        EnterpriseId = request.UnitId.Value
                    };
                    await _unitOfWork.Repository<EnterpriseUser>().AddAsync(enterpriseUser, cancellationToken);
                }

                var auditLog = new AuditLog
                {
                    AuditLogId = Guid.NewGuid(),
                    Action = AuditAction.Create,
                    EntityType = nameof(User),
                    EntityId = user.UserId,
                    PerformedById = auditorId,
                    Reason = $"Created User: {user.UserCode} | Role: {parsedRole}",
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Repository<AuditLog>().AddAsync(auditLog, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);

                // Send notification email
                await _emailSender.EnqueueAccountCreationEmailAsync(
                    user.Email,
                    user.FullName,
                    user.Email,
                    parsedRole.ToString(),
                    randomPassword,
                    user.UserId,
                    auditorId,
                    cancellationToken);

                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                
                await _cacheService.RemoveByPatternAsync("user:list", cancellationToken);

                _logger.LogInformation("Successfully created Admin User {UserCode} (ID: {UserId})", user.UserCode, user.UserId);

                var response = _mapper.Map<CreateAdminUserResponse>(user);
                return Result<CreateAdminUserResponse>.Success(response);
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw; // Rethrow to let global handler deal with it
            }
        }
    }
}
