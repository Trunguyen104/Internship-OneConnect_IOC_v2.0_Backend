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
            //Validate current user
            if (!Guid.TryParse(_currentUserService.UserId, out var auditorId))
            {
                return Result<CreateLogbookResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Users.InvalidAuditor),
                    ResultErrorType.Unauthorized
                );
            }

            //Validate internship exists
            var internshipExists = await _unitOfWork.Repository<InternshipGroup>()
              .ExistsAsync(i => i.InternshipId == request.InternshipId, cancellationToken);

            if(!internshipExists)
            {
               return Result<CreateLogbookResponse>.Failure(
                   _messageService.GetMessage(MessageKeys.Logbook.InvalidInternship),
                   ResultErrorType.BadRequest
               );
            }

            //Validate student is existing
            var isStudentExist = await _unitOfWork.Repository<Student>()
                    .ExistsAsync(s => s.StudentId == request.StudentId, cancellationToken);

            if(!isStudentExist)
            {
                return Result<CreateLogbookResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Users.NotFound),
                    ResultErrorType.BadRequest
                );
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                //map request to logbook entity
                var logbook = _mapper.Map<Logbook>(request);
                logbook.LogbookId = Guid.NewGuid();
                logbook.CreatedAt = DateTime.UtcNow;
                logbook.DateReport = request.DateReport;

                //determine logbook status based on report date and creation date
                if (logbook.DateReport.Date == logbook.CreatedAt.Date)
                {
                    logbook.Status = LogbookStatus.PUNCTUAL;
                }
                else
                {
                    logbook.Status = LogbookStatus.LATE;
                }

                await _unitOfWork.Repository<Logbook>()
                    .AddAsync(logbook, cancellationToken);

                await _unitOfWork.SaveChangeAsync(cancellationToken);

                //create audit log for logbook creation
                var auditLog = new AuditLog
                {
                    AuditLogId = Guid.NewGuid(),
                    Action = AuditAction.Create,
                    EntityType = nameof(Logbook),
                    EntityId = logbook.LogbookId,
                    PerformedById = auditorId,
                    Reason = $"Created logbook for internship {logbook.InternshipId}",
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Repository<AuditLog>()
                    .AddAsync(auditLog, cancellationToken);

                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                var response = _mapper.Map<CreateLogbookResponse>(logbook);
                return Result<CreateLogbookResponse>.Success(response);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);

                _logger.LogError(ex, "Error creating logbook");

                return Result<CreateLogbookResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Logbook.CreationFailed),
                    ResultErrorType.BadRequest
                );
            }
        }
    }
}
