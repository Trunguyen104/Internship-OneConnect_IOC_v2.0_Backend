using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Admin.Users.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;

using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Admin.Users.Commands.ToggleUserStatus
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
            _logger.LogInformation("Toggling status for User {UserId} to {NewStatus} by Auditor {AuditorId}", 
                request.UserId, request.NewStatus, _currentUserService.UserId);

            if (!Guid.TryParse(_currentUserService.UserId, out var auditorId))
            {
                _logger.LogWarning("Invalid Auditor ID: {AuditorId}", _currentUserService.UserId);
                return Result<ToggleUserStatusResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Users.InvalidAuditor),
                    ResultErrorType.Unauthorized
                );
            }

            var user = await _unitOfWork.Repository<User>()
                .GetByIdAsync(request.UserId, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found for status toggle", request.UserId);
                return Result<ToggleUserStatusResponse>.NotFound(_messageService.GetMessage(MessageKeys.Users.NotFound));
            }



            // Use rich domain method
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

            await _cacheService.RemoveByPatternAsync(AdminUserCacheKeys.UserListPattern(), cancellationToken);
            await _cacheService.RemoveAsync(AdminUserCacheKeys.User(user.UserId), cancellationToken);

            _logger.LogInformation("Successfully toggled status for User {UserCode} (ID: {UserId})", user.UserCode, user.UserId);

            var response = _mapper.Map<ToggleUserStatusResponse>(user);
            return Result<ToggleUserStatusResponse>.Success(response);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, "Failed to toggle status for User {UserId}", request.UserId);
                return Result<ToggleUserStatusResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.InternalServerError);
            }
        }
    }
}
