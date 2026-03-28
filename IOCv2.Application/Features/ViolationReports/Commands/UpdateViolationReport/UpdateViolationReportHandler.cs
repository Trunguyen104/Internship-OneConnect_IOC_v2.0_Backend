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

namespace IOCv2.Application.Features.ViolationReports.Commands.UpdateViolationReport
{
    /// <summary>
    /// Handles update requests for a ViolationReport.
    /// Responsibilities:
    /// - Load report with necessary navigation properties for validation and mapping
    /// - Enforce authorization rules (Mentor restrictions)
    /// - Validate OccurredDate against internship phase
    /// - Detect concurrent edits using LastUpdate/ForceUpdate
    /// - Apply changes inside a transaction and persist
    /// - Map and return the updated DTO or appropriate error
    /// </summary>
    public class UpdateViolationReportHandler : IRequestHandler<UpdateViolationReportCommand, Result<UpdateViolationReportResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly IBackgroundEmailSender _emailSender;
        private readonly ILogger<UpdateViolationReportHandler> _logger;
        private readonly IMapper _mapper;

        public UpdateViolationReportHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            IBackgroundEmailSender emailSender,
            ILogger<UpdateViolationReportHandler> logger,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _emailSender = emailSender;
            _logger = logger;
            _mapper = mapper;
        }

        /// <summary>
        /// Handle update:
        /// 1. Load report including Student.User and InternshipGroup.InternshipPhase for validations and mapping.
        /// 2. Ensure current user (if Mentor) is allowed to modify the report.
        /// 3. Validate OccurredDate falls within internship phase when available.
        /// 4. Check for optimistic concurrency (LastUpdate) unless ForceUpdate specified.
        /// 5. Perform update inside a transaction and commit/rollback on error.
        /// </summary>
        public async Task<Result<UpdateViolationReportResponse>> Handle(UpdateViolationReportCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation(_messageService.GetMessage(MessageKeys.ViolationReportKey.UpdatingViolationReportByUser), request.ViolationReportId, _currentUserService.UserId);

            // 1) Load report with navigation properties required for validation.
            var report = await _unitOfWork.Repository<ViolationReport>().Query()
                .Include(v => v.Student).ThenInclude(s => s.User)
                .Include(v => v.InternshipGroup).ThenInclude(g => g.InternshipPhase)
                .FirstOrDefaultAsync(v => v.ViolationReportId == request.ViolationReportId, cancellationToken);

            if (report == null)
            {
                _logger.LogWarning(_messageService.GetMessage(MessageKeys.ViolationReportKey.ViolationReportNotFound), request.ViolationReportId);
                return Result<UpdateViolationReportResponse>.Failure(_messageService.GetMessage(MessageKeys.ViolationReportKey.NotFound), ResultErrorType.NotFound);
            }

            // 2) Authorization: Mentors may only edit reports they created.
            if (UserRole.Mentor.ToString().Equals(_currentUserService.Role)
                && (!Guid.TryParse(_currentUserService.UserId, out var currentUserGuid) || report.CreatedBy != currentUserGuid))
            {
                _logger.LogWarning(_messageService.GetMessage(MessageKeys.ViolationReportKey.UserNotAllowedToUpdateViolationReport), _currentUserService.UserId, request.ViolationReportId);
                return Result<UpdateViolationReportResponse>.Failure(_messageService.GetMessage(MessageKeys.ViolationReportKey.NotAllowedToUpdateThisReport), ResultErrorType.Forbidden);
            }

            // 3) Validate OccurredDate against internship phase dates (if available).
            InternshipGroup? nullableGroup = report.InternshipGroup;
            var phase = nullableGroup?.InternshipPhase;
            if (phase != null)
            {
                if (request.OccurredDate < phase.StartDate)
                    return Result<UpdateViolationReportResponse>.Failure(_messageService.GetMessage(MessageKeys.ViolationReportKey.OccurredDateBeforeInternshipStart));
                if (request.OccurredDate > phase.EndDate)
                    return Result<UpdateViolationReportResponse>.Failure(_messageService.GetMessage(MessageKeys.ViolationReportKey.InternshipHasEnded));
            }

            // 4) Concurrency detection: if server record was updated after client's LastUpdate and ForceUpdate not set, return conflict with info.
            if (request.LastUpdate.HasValue && report.UpdatedAt.HasValue && !request.ForceUpdate)
            {
                if (report.UpdatedAt.Value > request.LastUpdate.Value)
                {
                    var updatedAtLocal = report.UpdatedAt.Value.ToLocalTime().ToString("g");
                    _logger.LogWarning(_messageService.GetMessage(MessageKeys.ViolationReportKey.ViolationReportUpdatedByAnotherUser), updatedAtLocal);
                    return Result<UpdateViolationReportResponse>.Failure(_messageService.GetMessage(MessageKeys.ViolationReportKey.ViolationReportUpdatedByAnotherUser, updatedAtLocal), ResultErrorType.Conflict);
                }
            }

            // 5) Proceed to update editable fields within a transaction.
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                // Only update allowed fields.
                report.StudentId = request.StudentId;
                report.Description = request.Description;
                report.OccurredDate = request.OccurredDate;

                // Persist changes.
                await _unitOfWork.Repository<ViolationReport>().UpdateAsync(report, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);

                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                // Map response (EF may have set additional fields like UpdatedAt).
                var response = _mapper.Map<UpdateViolationReportResponse>(report);
                return Result<UpdateViolationReportResponse>.Success(response, _messageService.GetMessage(MessageKeys.ViolationReportKey.ViolationReportUpdatedSuccessfully));
            }
            catch (Exception ex)
            {
                // Rollback transaction on any failure and return conflict error.
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, _messageService.GetMessage(MessageKeys.ViolationReportKey.FailedToUpdateViolationReport), request.ViolationReportId);
                return Result<UpdateViolationReportResponse>.Failure(_messageService.GetMessage(MessageKeys.ViolationReportKey.ErrorOccurredWhileUpdatingViolationReport), ResultErrorType.Conflict);
            }
        }
    }
}