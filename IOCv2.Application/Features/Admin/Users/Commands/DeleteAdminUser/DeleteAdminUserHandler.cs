using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Admin.Users.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Admin.Users.Commands.DeleteAdminUser
{
    public class DeleteAdminUserHandler : IRequestHandler<DeleteAdminUserCommand, Result<DeleteAdminUserResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<DeleteAdminUserHandler> _logger;

        public DeleteAdminUserHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            ICacheService cacheService,
            ILogger<DeleteAdminUserHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<Result<DeleteAdminUserResponse>> Handle(DeleteAdminUserCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Deleting Admin User {UserId} by Auditor {AuditorId}", 
                request.UserId, _currentUserService.UserId);

            if (!Guid.TryParse(_currentUserService.UserId, out var auditorId))
            {
                _logger.LogWarning("Invalid Auditor ID: {AuditorId}", _currentUserService.UserId);
                return Result<DeleteAdminUserResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Users.InvalidAuditor),
                    ResultErrorType.Unauthorized
                );
            }

            var user = await _unitOfWork.Repository<User>()
                .GetByIdAsync(request.UserId, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found for deletion", request.UserId);
                return Result<DeleteAdminUserResponse>.NotFound(_messageService.GetMessage(MessageKeys.Users.NotFound));
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                // Soft delete is handled by AppDbContext / BaseEntity (sets DeletedAt)
                await _unitOfWork.Repository<User>().DeleteAsync(user, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);

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

                await _cacheService.RemoveByPatternAsync(AdminUserCacheKeys.UserListPattern(), cancellationToken);
                await _cacheService.RemoveAsync(AdminUserCacheKeys.User(user.UserId), cancellationToken);

                _logger.LogInformation("Successfully deleted Admin User {UserCode} (ID: {UserId})", user.UserCode, user.UserId);

                var response = _mapper.Map<DeleteAdminUserResponse>(user);
                return Result<DeleteAdminUserResponse>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete Admin User {UserId}", request.UserId);
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Result<DeleteAdminUserResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.InternalError),
                    ResultErrorType.InternalServerError);
            }
        }
    }
}
