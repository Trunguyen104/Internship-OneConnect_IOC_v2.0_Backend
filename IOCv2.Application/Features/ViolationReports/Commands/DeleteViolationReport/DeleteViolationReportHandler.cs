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
using System.Threading.Tasks;

namespace IOCv2.Application.Features.ViolationReports.Commands.DeleteViolationReport
{
    /// <summary>
    /// Handles deletion of a ViolationReport.
    /// Responsibilities:
    /// - Load the report with necessary navigation properties for authorization and mapping.
    /// - Enforce Mentor scoping: Mentors can only delete reports for their groups.
    /// - Map the response DTO before deletion (so entity data is available).
    /// - Perform the delete inside a transaction and commit or rollback on failure.
    /// </summary>
    public class DeleteViolationReportHandler : IRequestHandler<DeleteViolationReportCommand, Result<DeleteViolationReportResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ILogger<DeleteViolationReportHandler> _logger;

        public DeleteViolationReportHandler(IUnitOfWork unitOfWork, IMapper mapper, ICurrentUserService currentUserService, IMessageService messageService, ILogger<DeleteViolationReportHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _logger = logger;
        }

        /// <summary>
        /// Handle deletion:
        /// 1. Load violation report with Student.User and InternshipGroup.Mentor.User for authorization & mapping.
        /// 2. If not found, return NotFound.
        /// 3. If current user is Mentor, ensure they are owner of the group that created the report.
        /// 4. Map the response DTO before deleting, then delete inside a transaction.
        /// 5. Commit or rollback on error, logging appropriately.
        /// </summary>
        public async Task<Result<DeleteViolationReportResponse>> Handle(DeleteViolationReportCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1) Load the report with related entities needed for authorization and mapping.
                var violationReport = await _unitOfWork.Repository<Domain.Entities.ViolationReport>().Query().Include(vr => vr.Student).ThenInclude(s => s.User)
                    .Include(vr => vr.InternshipGroup).ThenInclude(ig => ig.Mentor)
                            .ThenInclude(m => m!.User).FirstOrDefaultAsync(vr => vr.ViolationReportId == request.ViolationReportId, cancellationToken);

                // 2) Not found handling.
                if (violationReport is null)
                {
                    _logger.LogWarning(_messageService.GetMessage(MessageKeys.ViolationReportKey.ViolationReportLogNotFound), request.ViolationReportId);
                    return Result<DeleteViolationReportResponse>.NotFound(_messageService.GetMessage(MessageKeys.Common.NotFound));
                }

                // 3) Authorization: if the user is a Mentor, ensure they are the mentor of the group that owns this report.
                var role = _currentUserService.Role ?? string.Empty;
                if (UserRole.Mentor.ToString().Equals(role))
                {
                    var mentorUserId = violationReport.InternshipGroup?.Mentor?.UserId;
                    if (mentorUserId == null || mentorUserId != Guid.Parse(_currentUserService.UserId!))
                    {
                        _logger.LogWarning(_messageService.GetMessage(MessageKeys.ViolationReportKey.LogDeleteNotOwner), Guid.Parse(_currentUserService.UserId!), request.ViolationReportId);
                        return Result<DeleteViolationReportResponse>.Failure(_messageService.GetMessage(MessageKeys.ViolationReportKey.DeleteNotOwner), ResultErrorType.Forbidden);
                    }
                }

                // 4) Map the response DTO BEFORE deletion while entity is still in memory.
                var response = _mapper.Map<DeleteViolationReportResponse>(violationReport);

                // 5) Perform delete in a transaction to ensure data integrity.
                await _unitOfWork.BeginTransactionAsync(cancellationToken);
                await _unitOfWork.Repository<Domain.Entities.ViolationReport>().DeleteAsync(violationReport, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation(_messageService.GetMessage(MessageKeys.ViolationReportKey.ViolationReportDeletedSuccessfully), violationReport.ViolationReportId);
                return Result<DeleteViolationReportResponse>.Success(response);
            }
            catch (Exception ex)
            {
                // On any exception, rollback transaction and return a conflict error with logging.
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, _messageService.GetMessage(MessageKeys.ViolationReportKey.ErrorOccurredWhileDeletingViolationReport), request.ViolationReportId);
                return Result<DeleteViolationReportResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.Conflict);
            }
        }
    }
}
