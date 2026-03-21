using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Extensions.ViolationReport;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.ViolationReports.Commands.CreateViolationReport
{
    /// <summary>
    /// Handles creation of a violation report.
    /// Ensures the requesting user is allowed to create the report, validates internship membership/date constraints,
    /// persists the report within a transaction, and returns a DTO response.
    /// </summary>
    public class CreateViolationReportHandler : IRequestHandler<CreateViolationReportCommand, Result<CreateViolationReportResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ILogger<CreateViolationReportHandler> _logger;
        private readonly IMapper _mapper;

        /// <summary>
        /// ctor - dependencies injected for data access, current user info, messaging and logging.
        /// </summary>
        public CreateViolationReportHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IMessageService messageService, ILogger<CreateViolationReportHandler> logger, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _logger = logger;
            _mapper = mapper;
        }

        /// <summary>
        /// Validate input, perform authorization checks, create and persist the ViolationReport entity inside a transaction,
        /// then build and return a response DTO. On error, the transaction is rolled back and an error result is returned.
        /// </summary>
        public async Task<Result<CreateViolationReportResponse>> Handle(CreateViolationReportCommand request, CancellationToken cancellationToken)
        {
            // Start a database transaction to ensure atomicity of operations (create + notifications, etc.)
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                // Load the student (include related User for name) to validate existence.
                var student = await _unitOfWork.Repository<Student>().Query().Where(s => s.StudentId == request.StudentId)
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(cancellationToken);

                // If student does not exist, return a NotFound result with a localized message.
                if (student == null)
                    return Result<CreateViolationReportResponse>.Failure(_messageService.GetMessage(MessageKeys.StudentMessageKey.StudentNotFound), ResultErrorType.NotFound);

                // Find the internship id for the student (mapping table InternshipStudent).
                var internshipStudentId = await _unitOfWork.Repository<InternshipStudent>().Query().Where(s => s.StudentId == request.StudentId)
                    .Select(s => s.InternshipId).FirstOrDefaultAsync(cancellationToken);

                // Load the internship group (include Term for date boundaries).
                var group = await _unitOfWork.Repository<InternshipGroup>().Query().Where(g => g.InternshipId == internshipStudentId)
                    .Include(g => g.Term)
                    .FirstOrDefaultAsync(cancellationToken);

                // If group not found, return NotFound with a localized message.
                if (group == null)
                    return Result<CreateViolationReportResponse>.Failure(_messageService.GetMessage(MessageKeys.InternshipGroups.NotFound), ResultErrorType.NotFound);

                // Authorization: mentors are only allowed to report for students in their own group.
                if (UserRole.Mentor.ToString().Equals(_currentUserService.Role) && group.MentorId != Guid.Parse(_currentUserService.UserId!))
                    return Result<CreateViolationReportResponse>.Failure(_messageService.GetMessage(MessageKeys.ViolationReportKey.NotAllowedToReport), ResultErrorType.Forbidden);

                // Validate OccurredDate against internship start date (if set).
                if (group.StartDate.HasValue && request.OccurredDate < group.Term.StartDate)
                    return Result<CreateViolationReportResponse>.Failure(_messageService.GetMessage(MessageKeys.ViolationReportKey.OccurredDateCannotBeBeforeInternshipStart), ResultErrorType.BadRequest);

                // Validate OccurredDate against internship end date (if set).
                if (group.EndDate.HasValue && request.OccurredDate > group.Term.EndDate)
                    return Result<CreateViolationReportResponse>.Failure(_messageService.GetMessage(MessageKeys.ViolationReportKey.OccurredDateCannotBeAfterInternshipEnd), ResultErrorType.BadRequest);

                // Build the ViolationReport entity to persist.
                var report = new ViolationReport
                {
                    ViolationReportId = Guid.NewGuid(),
                    StudentId = request.StudentId,
                    InternshipGroupId = group.InternshipId,
                    OccurredDate = request.OccurredDate,
                    Description = request.Description,
                };

                // Persist the report.
                await _unitOfWork.Repository<ViolationReport>().AddAsync(report, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);

                //chờ notification 

                // Commit the transaction once all operations succeeded.
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                // Resolve the name of the user who created the report (CreatedBy could be null; handle gracefully).
                var createdByName = await _unitOfWork.Repository<User>().Query().Where(u => u.UserId == report.CreatedBy)
                    .Select(u => u.FullName).FirstOrDefaultAsync(cancellationToken);

                // Map the persisted entity into the response DTO.
                var response = new CreateViolationReportResponse
                {
                    ViolationReportId = report.ViolationReportId,
                    StudentId = report.StudentId,
                    OccurredDate = report.OccurredDate,
                    Description = report.Description,
                    StudentName = student.User.FullName,
                    CreatedBy = createdByName!,
                    GroupName = group.GroupName
                };

                // Return success with a localized success message.
                return Result<CreateViolationReportResponse>.Success(response, _messageService.GetMessage(MessageKeys.ViolationReportKey.CreateViolationReportSuccess));
            }
            catch (Exception ex)
            {
                // Roll back transaction on any error and log the exception with a localized error message.
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, _messageService.GetMessage(MessageKeys.ViolationReportKey.CreateViolationReportError));

                // Return a generic internal server error result with a localized message.
                return Result<CreateViolationReportResponse>.Failure(_messageService.GetMessage(MessageKeys.ViolationReportKey.CreateViolationReportError), ResultErrorType.InternalServerError);
            }
        }
    }
}