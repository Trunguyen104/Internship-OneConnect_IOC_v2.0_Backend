using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
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
            if (!Guid.TryParse(_currentUserService.UserId, out var auditorId))
            {
                return Result<CreateAdminUserResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Users.InvalidAuditor),
                    ResultErrorType.Unauthorized
                );
            }

            // Parse Role
            if (!Enum.TryParse<UserRole>(request.Role, true, out var parsedRole))
            {
                return Result<CreateAdminUserResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InvalidRequest));
            }

            // Business Rules for delegated management
            var auditorRoleStr = _currentUserService.Role;
            if (Enum.TryParse<UserRole>(auditorRoleStr, true, out var auditorRole))
            {
                if (auditorRole == UserRole.SchoolAdmin)
                {
                    if (parsedRole != UserRole.Student)
                        return Result<CreateAdminUserResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.AccessDenied));
                    
                    // SchoolAdmin must link to their own university
                    // For now, we trust the UnitId provided or we should fetch auditor's UnitId
                    // To be safe, let's assume SuperAdmin provides UnitId, but for SchoolAdmin we might want to auto-assign it.
                }
                else if (auditorRole == UserRole.EnterpriseAdmin)
                {
                    if (parsedRole != UserRole.HR && parsedRole != UserRole.Mentor)
                        return Result<CreateAdminUserResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.AccessDenied));
                }
                else if (auditorRole != UserRole.SuperAdmin && auditorRole != UserRole.Moderator)
                {
                    return Result<CreateAdminUserResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.AccessDenied));
                }
            }
            var emailExists = await _unitOfWork.Repository<User>()
                .ExistsAsync(u => u.Email == request.Email, cancellationToken);
            if (emailExists)
            {
                return Result<CreateAdminUserResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Users.EmailConflict),
                    ResultErrorType.Conflict
                );
            }

            // Validate unit exists for role that requires it
            if ((parsedRole == UserRole.SchoolAdmin || parsedRole == UserRole.Student) && request.UnitId.HasValue)
            {
                var universityExists = await _unitOfWork.Repository<University>()
                    .ExistsAsync(u => u.UniversityId == request.UnitId.Value, cancellationToken);
                if (!universityExists)
                    return Result<CreateAdminUserResponse>.Failure(_messageService.GetMessage(MessageKeys.University.NotFound), ResultErrorType.NotFound);
            }
            else if ((parsedRole == UserRole.EnterpriseAdmin || parsedRole == UserRole.HR || parsedRole == UserRole.Mentor) && request.UnitId.HasValue)
            {
                var enterpriseExists = await _unitOfWork.Repository<Enterprise>()
                    .ExistsAsync(e => e.EnterpriseId == request.UnitId.Value, cancellationToken);
                if (!enterpriseExists)
                    return Result<CreateAdminUserResponse>.Failure(_messageService.GetMessage(MessageKeys.Enterprise.NotFound), ResultErrorType.NotFound);
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                // Create user
                var user = _mapper.Map<User>(request);
                user.UserId = Guid.NewGuid();
                user.Role = parsedRole;
                user.UserCode = await _userServices.GenerateUserCodeAsync(parsedRole, cancellationToken);
                user.Status = UserStatus.Active;

                // Generate random password if not provided
                var randomPassword = _passwordService.GenerateRandomPassword();
                user.PasswordHash = _passwordService.HashPassword(randomPassword);

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

                    // If it's a student, also create Student record
                    if (parsedRole == UserRole.Student)
                    {
                        var student = new Student
                        {
                            StudentId = Guid.NewGuid(),
                            UserId = user.UserId,
                            Status = StudentStatus.NO_INTERNSHIP
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
                    Reason = $"Created user {user.UserCode} with role {parsedRole}",
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
                
                // Clear cache
                await _cacheService.RemoveByPatternAsync("user:list", cancellationToken);

                var response = _mapper.Map<CreateAdminUserResponse>(user);
                return Result<CreateAdminUserResponse>.Success(response);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }
    }
}
