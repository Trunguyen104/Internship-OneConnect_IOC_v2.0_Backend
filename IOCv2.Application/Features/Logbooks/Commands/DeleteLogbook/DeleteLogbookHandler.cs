using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Admin.UserManagement.Commands.DeleteUser;
using IOCv2.Application.Features.Logbooks.Commands.CreateLogbook;
using IOCv2.Application.Features.Logbooks.Common;
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
            _logger.LogInformation("Starting logbook deletion for LogbookId: {LogbookId}", request.LogbookId);

            // 1. Validate current user
            if (!Guid.TryParse(_currentUserService.UserId, out var userId))
            {
                var message = _messageService.GetMessage(MessageKeys.Users.InvalidAuditor);
                _logger.LogWarning("Unauthorized access attempt: {UserId}", _currentUserService.UserId);
                return Result<DeleteLogbookResponse>.Failure(message, ResultErrorType.Unauthorized);
            }

            // 2. Fetch logbook with existence check
            var logbook = (await _unitOfWork.Repository<Domain.Entities.Logbook>()
                .FindAsync(l => l.LogbookId == request.LogbookId, cancellationToken)).FirstOrDefault();


            if (logbook == null)
            {
                _logger.LogWarning("Logbook not found: {LogbookId}", request.LogbookId);
                return Result<DeleteLogbookResponse>.Failure(_messageService.GetMessage(MessageKeys.Logbooks.NotFound), ResultErrorType.NotFound);
            }

            // 3. Security: Ownership Validation (FFA-SEC)
            var student = (await _unitOfWork.Repository<Student>()
                    .FindAsync(s => s.UserId == userId, cancellationToken)).FirstOrDefault();

            if (student == null || logbook.StudentId != student.StudentId)
            {
                _logger.LogWarning("User {UserId} attempted to delete logbook {LogbookId} belonging to another student", userId, request.LogbookId);
                return Result<DeleteLogbookResponse>.Failure(_messageService.GetMessage(MessageKeys.Logbooks.DeleteForbidden), ResultErrorType.Forbidden);
            }

            try
            {
                // 4. Transaction & Deletion (FF-TXG)
                await _unitOfWork.BeginTransactionAsync(cancellationToken);
                
                // Soft delete is handled by AppDbContext / BaseEntity (sets DeletedAt)
                await _unitOfWork.Repository<Domain.Entities.Logbook>().DeleteAsync(logbook, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);

                var auditLog = new AuditLog
                {
                    AuditLogId = Guid.NewGuid(),
                    Action = AuditAction.Delete,
                    EntityType = nameof(Logbook),
                    EntityId = logbook.LogbookId,
                    PerformedById = userId,
                    Reason = "Deleted logbook",
                    CreatedAt = DateTime.UtcNow,
                };
                await _unitOfWork.Repository<AuditLog>().AddAsync(auditLog, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);

                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                // 5. Post-Commit Actions (Cache clearing)
                await _cacheService.RemoveByPatternAsync(LogbookCacheKeys.LogbookListPattern(logbook.InternshipId), cancellationToken);
                await _cacheService.RemoveAsync(LogbookCacheKeys.Logbook(logbook.LogbookId), cancellationToken);

                _logger.LogInformation("Logbook {LogbookId} deleted successfully", logbook.LogbookId);

                var response = _mapper.Map<DeleteLogbookResponse>(logbook);
                return Result<DeleteLogbookResponse>.Success(response);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, "Failed to delete logbook {LogbookId}", request.LogbookId);
                return Result<DeleteLogbookResponse>.Failure("An error occurred while deleting the logbook.", ResultErrorType.Conflict);
            }
        }
    }
}
