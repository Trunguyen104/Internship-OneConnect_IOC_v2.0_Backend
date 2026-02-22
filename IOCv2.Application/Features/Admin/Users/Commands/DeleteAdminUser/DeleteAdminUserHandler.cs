using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;

namespace IOCv2.Application.Features.Admin.Users.Commands.DeleteAdminUser
{
    public class DeleteAdminUserHandler : IRequestHandler<DeleteAdminUserCommand, Result<DeleteAdminUserResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ICacheService _cacheService;

        public DeleteAdminUserHandler(
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

        public async Task<Result<DeleteAdminUserResponse>> Handle(DeleteAdminUserCommand request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var auditorId))
            {
                return Result<DeleteAdminUserResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Users.InvalidAuditor),
                    ResultErrorType.Unauthorized
                );
            }

            var user = await _unitOfWork.Repository<User>()
                .GetByIdAsync(request.UserId, cancellationToken);

            if (user == null)
                return Result<DeleteAdminUserResponse>.NotFound(_messageService.GetMessage(MessageKeys.Users.NotFound));

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

                await _cacheService.RemoveByPatternAsync("user:list", cancellationToken);
                await _cacheService.RemoveAsync($"user:{user.UserId}", cancellationToken);

                var response = _mapper.Map<DeleteAdminUserResponse>(user);
                return Result<DeleteAdminUserResponse>.Success(response);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }
    }
}
