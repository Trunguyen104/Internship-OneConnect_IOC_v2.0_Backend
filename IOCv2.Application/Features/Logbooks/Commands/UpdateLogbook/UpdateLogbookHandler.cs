using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
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

namespace IOCv2.Application.Features.Logbooks.Commands.UpdateLogbook
{
    public class UpdateLogbookHandler : IRequestHandler<UpdateLogbookCommand, Result<UpdateLogbookResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ILogger<UpdateLogbookHandler> _logger;
        private readonly ICacheService _cacheService;

        public UpdateLogbookHandler(IUnitOfWork unitOfWork, IMapper mapper, ICurrentUserService currentUserService, IMessageService messageService, ILogger<UpdateLogbookHandler> logger, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _logger = logger;
            _cacheService = cacheService;
        }

        public async Task<Result<UpdateLogbookResponse>> Handle(UpdateLogbookCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting logbook update for LogbookId: {LogbookId}", request.LogbookId);

            // 1. Validate current user
            if (!Guid.TryParse(_currentUserService.UserId, out var userId))
            {
                var message = _messageService.GetMessage(MessageKeys.Users.InvalidAuditor);
                _logger.LogWarning("Unauthorized access attempt or invalid user ID: {UserId}", _currentUserService.UserId);
                return Result<UpdateLogbookResponse>.Failure(message, ResultErrorType.Unauthorized);
            }

            // 2. Fetch logbook with ownership check
            var logbook = (await _unitOfWork.Repository<Logbook>()
                    .FindAsync(l => l.LogbookId == request.LogbookId && l.InternshipId == request.InternshipId, cancellationToken)).FirstOrDefault();


            if (logbook == null)
            {
                var message = _messageService.GetMessage(MessageKeys.Logbooks.NotFound);
                _logger.LogWarning("Logbook not found: {LogbookId}", request.LogbookId);
                return Result<UpdateLogbookResponse>.Failure(message, ResultErrorType.NotFound);
            }

            // 3. Security: Ownership Validation (FFA-SEC)
            var student = (await _unitOfWork.Repository<Student>()
                    .FindAsync(s => s.UserId == userId, cancellationToken)).FirstOrDefault();

            if (student == null || logbook.StudentId != student.StudentId)
            {
                _logger.LogWarning("User {UserId} attempted to update logbook {LogbookId} belonging to another student", userId, request.LogbookId);
                return Result<UpdateLogbookResponse>.Failure(_messageService.GetMessage(MessageKeys.Logbooks.UpdateForbidden), ResultErrorType.Forbidden);
            }

            // 4. Update via Domain Method (Architecture: FFA-CAG)
            logbook.Update(
                request.Summary,
                request.Issue,
                request.Plan,
                request.DateReport);

            try
            {
                // 5. Transaction & Writing (FF-TXG)
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                await _unitOfWork.Repository<Logbook>().UpdateAsync(logbook, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);

                // Create audit log
                var auditLog = new AuditLog
                {
                    AuditLogId = Guid.NewGuid(),
                    Action = AuditAction.Update,
                    EntityType = nameof(Logbook),
                    EntityId = logbook.LogbookId,
                    PerformedById = userId,
                    Reason = $"Updated logbook for internship group {logbook.InternshipId}",
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Repository<AuditLog>().AddAsync(auditLog, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);

                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                await _cacheService.RemoveByPatternAsync(LogbookCacheKeys.LogbookListPattern(logbook.InternshipId), cancellationToken);
                await _cacheService.RemoveAsync(LogbookCacheKeys.Logbook(logbook.LogbookId), cancellationToken);

                _logger.LogInformation("Logbook {LogbookId} updated successfully", logbook.LogbookId);

                // 6. Return response
                var response = _mapper.Map<UpdateLogbookResponse>(logbook);
                return Result<UpdateLogbookResponse>.Success(response);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, "Failed to update logbook {LogbookId}", request.LogbookId);
                return Result<UpdateLogbookResponse>.Failure("An error occurred while updating the logbook. Please try again later.", ResultErrorType.Conflict);
            }
        }
    }
}
