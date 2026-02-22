using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;

namespace IOCv2.Application.Features.Admin.Users.Commands.ToggleUserStatus
{
    public class ToggleUserStatusHandler : IRequestHandler<ToggleUserStatusCommand, Result<ToggleUserStatusResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ICacheService _cacheService;

        public ToggleUserStatusHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _cacheService = cacheService;
        }

        public async Task<Result<ToggleUserStatusResponse>> Handle(ToggleUserStatusCommand request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var auditorId))
            {
                return Result<ToggleUserStatusResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Users.InvalidAuditor),
                    ResultErrorType.Unauthorized
                );
            }

            var user = await _unitOfWork.Repository<User>()
                .GetByIdAsync(request.UserId, cancellationToken);

            if (user == null)
                return Result<ToggleUserStatusResponse>.NotFound(_messageService.GetMessage(MessageKeys.Users.NotFound));

            // Parse NewStatus
            if (!Enum.TryParse<UserStatus>(request.NewStatus, true, out var parsedStatus))
            {
                return Result<ToggleUserStatusResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InvalidRequest));
            }

            user.Status = parsedStatus;
            await _unitOfWork.Repository<User>().UpdateAsync(user, cancellationToken);

            var auditLog = new AuditLog
            {
                AuditLogId = Guid.NewGuid(),
                Action = AuditAction.Update,
                EntityType = nameof(User),
                EntityId = user.UserId,
                PerformedById = auditorId,
                Reason = $"Toggled user {user.UserCode} status to {parsedStatus}",
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Repository<AuditLog>().AddAsync(auditLog, cancellationToken);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            await _cacheService.RemoveByPatternAsync("user:list", cancellationToken);
            await _cacheService.RemoveAsync($"user:{user.UserId}", cancellationToken);

            var response = _mapper.Map<ToggleUserStatusResponse>(user);
            return Result<ToggleUserStatusResponse>.Success(response);
        }
    }
}
