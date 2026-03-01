using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Admin.Users.Commands.DeleteAdminUser;
using IOCv2.Application.Features.Logbooks.Commands.CreateLogbook;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static IOCv2.Application.Constants.MessageKeys;

namespace IOCv2.Application.Features.Logbooks.Commands.DeleteLogbook
{
    public class DeleteLogbookHandler : IRequestHandler<DeleteLogbookCommand, Result<DeleteLogbookResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<DeleteLogbookHandler> _logger;

        public DeleteLogbookHandler(IUnitOfWork unitOfWork, IMapper mapper, ICurrentUserService currentUserService, IMessageService messageService, ICacheService cacheService, ILogger<DeleteLogbookHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<Result<DeleteLogbookResponse>> Handle(DeleteLogbookCommand request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var auditorId))
            {
                return Result<DeleteLogbookResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Users.InvalidAuditor),
                    ResultErrorType.Unauthorized
                );
            }

            var logbook = await _unitOfWork.Repository<Domain.Entities.Logbook>()
                .GetByIdAsync(request.LogbookId, cancellationToken);

            if (logbook == null)
                return Result<DeleteLogbookResponse>.NotFound(_messageService.GetMessage(MessageKeys.Logbook.NotFound));

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                // Soft delete is handled by AppDbContext / BaseEntity (sets DeletedAt)
                await _unitOfWork.Repository<Domain.Entities.Logbook>().DeleteAsync(logbook, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);

                var auditLog = new AuditLog
                {
                    AuditLogId = Guid.NewGuid(),
                    Action = AuditAction.Delete,
                    EntityType = nameof(User),
                    EntityId = logbook.LogbookId,
                    PerformedById = auditorId,
                    Reason = $"Deleted logbook",
                    CreatedAt = DateTime.UtcNow,
                };
                await _unitOfWork.Repository<AuditLog>().AddAsync(auditLog, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);

                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                await _cacheService.RemoveByPatternAsync("logbook:list", cancellationToken);
                await _cacheService.RemoveAsync($"logbook:{logbook.LogbookId}", cancellationToken);

                var response = _mapper.Map<DeleteLogbookResponse>(logbook);
                return Result<DeleteLogbookResponse>.Success(response);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);

                _logger.LogError(ex, "Error deleting logbook");

                return Result<DeleteLogbookResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Logbook.DeleteFailed),
                    ResultErrorType.BadRequest
                );
            }
        }
    }
}
