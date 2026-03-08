using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
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

namespace IOCv2.Application.Features.Logbooks.Commands.CreateLogbook
{
    public class CreateLogbookHandler : IRequestHandler<CreateLogbookCommand, Result<CreateLogbookResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ILogger<CreateLogbookHandler> _logger;

        public CreateLogbookHandler(IUnitOfWork unitOfWork, IMapper mapper, ICurrentUserService currentUserService, IMessageService messageService, ILogger<CreateLogbookHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _logger = logger;
        }

        public async Task<Result<CreateLogbookResponse>> Handle(CreateLogbookCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting logbook creation process for internship {InternshipId} on date {DateReport}", request.InternshipId, request.DateReport);

            // 1. Validate current user
            if (!Guid.TryParse(_currentUserService.UserId, out var auditorId))
            {
                var message = _messageService.GetMessage(MessageKeys.Users.InvalidAuditor);
                _logger.LogWarning("Invalid auditor ID: {UserId}", _currentUserService.UserId);
                return Result<CreateLogbookResponse>.Failure(message, ResultErrorType.Unauthorized);
            }

            // 2. Validate internship exists
            var internshipExists = await _unitOfWork.Repository<InternshipGroup>()
                .ExistsAsync(i => i.InternshipId == request.InternshipId, cancellationToken);
            if (!internshipExists)
            {
                _logger.LogWarning("Internship {InternshipId} not found", request.InternshipId);
                return Result<CreateLogbookResponse>.Failure("Internship group not found", ResultErrorType.BadRequest);
            }

            // 3. Validate student record
            var student = (await _unitOfWork.Repository<Student>()
                    .FindAsync(s => s.UserId == auditorId, cancellationToken)).FirstOrDefault();
            if (student == null)
            {
                _logger.LogWarning("Student record not found for user {UserId}", auditorId);
                return Result<CreateLogbookResponse>.Failure(
                    $"Student record not found for logged in user {auditorId}. Ensure you are logged in with a Student account.",
                    ResultErrorType.Unauthorized
                );
            }

            // 4. Create entity via Domain Factory (Architecture: FFA-CAG)
            var logbook = Logbook.Create(
                request.InternshipId,
                student.StudentId,
                request.Summary,
                request.Issue,
                request.Plan,
                request.DateReport);

            try
            {
                // 5. Transaction & Writing (FF-TXG)
                await _unitOfWork.BeginTransactionAsync(cancellationToken);
                
                await _unitOfWork.Repository<Logbook>().AddAsync(logbook, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);

                // Audit
                var auditLog = new AuditLog
                {
                    AuditLogId = Guid.NewGuid(),
                    Action = AuditAction.Create,
                    EntityType = nameof(Logbook),
                    EntityId = logbook.LogbookId,
                    PerformedById = auditorId,
                    Reason = $"Created logbook for internship group {logbook.InternshipId}",
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Repository<AuditLog>().AddAsync(auditLog, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);

                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                
                _logger.LogInformation("Logbook {LogbookId} created successfully for student {StudentId}", logbook.LogbookId, student.StudentId);

                // 6. Return response
                var response = _mapper.Map<CreateLogbookResponse>(logbook);
                return Result<CreateLogbookResponse>.Success(response);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, "Failed to create logbook for internship {InternshipId}", request.InternshipId);
                throw;
            }
        }
    }
}
