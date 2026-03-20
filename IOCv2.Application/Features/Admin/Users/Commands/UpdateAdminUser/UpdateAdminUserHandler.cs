using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Admin.Users.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace IOCv2.Application.Features.Admin.Users.Commands.UpdateAdminUser
{
    public class UpdateAdminUserHandler : IRequestHandler<UpdateAdminUserCommand, Result<UpdateAdminUserResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<UpdateAdminUserHandler> _logger;

        public UpdateAdminUserHandler(
            IUnitOfWork unitOfWork, 
            IMapper mapper, 
            ICurrentUserService currentUserService, 
            IMessageService messageService, 
            ICacheService cacheService,
            ILogger<UpdateAdminUserHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<Result<UpdateAdminUserResponse>> Handle(UpdateAdminUserCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Updating Admin User {UserId} by Auditor {AuditorId}", 
                request.UserId, _currentUserService.UserId);

            if (!Guid.TryParse(_currentUserService.UserId, out var auditorId))
            {
                _logger.LogWarning("Invalid Auditor ID: {AuditorId}", _currentUserService.UserId);
                return Result<UpdateAdminUserResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Users.InvalidAuditor),
                    ResultErrorType.Unauthorized
                );
            }

            var user = await _unitOfWork.Repository<User>()
                .Query()
                .Include(u => u.Student)
                .FirstOrDefaultAsync(u => u.UserId == request.UserId, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found for update", request.UserId);
                return Result<UpdateAdminUserResponse>.Failure(_messageService.GetMessage(MessageKeys.Users.NotFound));
            }

            if (user.Status == UserStatus.Inactive && request.Status != UserStatus.Active)
            {
                _logger.LogWarning("Attempted to update inactive user {UserId} without activation", request.UserId);
                return Result<UpdateAdminUserResponse>.Failure(_messageService.GetMessage(MessageKeys.Users.NotActive));
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                DateOnly? parsedDob = null;
                if (!string.IsNullOrWhiteSpace(request.DateOfBirth) && DateOnly.TryParse(request.DateOfBirth, out var dobVal))
                {
                    parsedDob = dobVal;
                }

                // Update using rich domain method
                user.UpdateProfile(
                    request.FullName,
                    request.PhoneNumber,
                    request.AvatarUrl,
                    request.Gender,
                    parsedDob
                );

                // Update Status if provided
                if (request.Status.HasValue)
                {
                    user.SetStatus(request.Status.Value);
                }

                // Update Student fields if applicable
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

                // Clear cache
                await _cacheService.RemoveAsync(AdminUserCacheKeys.User(user.UserId), cancellationToken);
                await _cacheService.RemoveByPatternAsync(AdminUserCacheKeys.UserListPattern(), cancellationToken);

                _logger.LogInformation("Successfully updated Admin User {UserCode} (ID: {UserId})", user.UserCode, user.UserId);

                var response = _mapper.Map<UpdateAdminUserResponse>(user);
                return Result<UpdateAdminUserResponse>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update Admin User {UserId}", request.UserId);
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Result<UpdateAdminUserResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.InternalError),
                    ResultErrorType.InternalServerError);
            }
        }
    }
}
