using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Extensions.Jobs;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Jobs.Commands.UpdateJobApplicationStatus
{
    public class UpdateInternshipApplicationStatusHandler : IRequestHandler<UpdateInternshipApplicationStatusCommand, Result<UpdateInternshipApplicationStatusResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IBackgroundEmailSender _emailSender;
        private readonly IMessageService _messageService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<UpdateInternshipApplicationStatusHandler> _logger;

        public UpdateInternshipApplicationStatusHandler(
            IUnitOfWork unitOfWork,
            IBackgroundEmailSender emailSender,
            IMessageService messageService,
            ICurrentUserService currentUserService,
            ILogger<UpdateInternshipApplicationStatusHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _emailSender = emailSender;
            _messageService = messageService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<Result<UpdateInternshipApplicationStatusResponse>> Handle(UpdateInternshipApplicationStatusCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Updating application status: {ApplicationId} => {NewStatus} by {UserId}", request.ApplicationId, request.NewStatus, _currentUserService.UserId);

            var application = await _unitOfWork.Repository<InternshipApplication>()
                .Query()
                .Include(a => a.Job)
                    .ThenInclude(j => j.Enterprise)
                .Include(a => a.Student)
                    .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(a => a.ApplicationId == request.ApplicationId, cancellationToken);

            if (application == null)
            {
                _logger.LogWarning("Application not found: {ApplicationId}", request.ApplicationId);
                return Result<UpdateInternshipApplicationStatusResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.RecordNotFound), ResultErrorType.NotFound);
            }

            // Security: only HR of the enterprise owning the job may update application (per AC-09)
            if (JobsPostingParam.GetJobPostings.EnterpriseRoles.Contains(_currentUserService.Role))
            {
                if (!Guid.TryParse(_currentUserService.UserId, out var userId))
                {
                    return Result<UpdateInternshipApplicationStatusResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.Unauthorized), ResultErrorType.Unauthorized);
                }

                var entUser = await _unitOfWork.Repository<EnterpriseUser>()
                    .Query()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.UserId == userId, cancellationToken);

                if (entUser == null || entUser.EnterpriseId != application.EnterpriseId)
                {
                    return Result<UpdateInternshipApplicationStatusResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.Forbidden), ResultErrorType.Forbidden);
                }
            }
            else
            {
                // Not an enterprise user -> forbid
                return Result<UpdateInternshipApplicationStatusResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.Forbidden), ResultErrorType.Forbidden);
            }

            var current = application.Status;

            // Terminal statuses are read-only
            var terminalStatuses = new[]
            {
                InternshipApplicationStatus.Placed,
                InternshipApplicationStatus.Rejected,
                InternshipApplicationStatus.Withdrawn
            };

            if (terminalStatuses.Contains(current))
            {
                return Result<UpdateInternshipApplicationStatusResponse>.Failure(_messageService.GetMessage("Application.ReadOnlyTerminal"), ResultErrorType.BadRequest);
            }

            // Allowed transitions per AC-09 (MVP)
            var allowed = current switch
            {
                InternshipApplicationStatus.Applied => new[] { InternshipApplicationStatus.Interviewing, InternshipApplicationStatus.Rejected },
                InternshipApplicationStatus.Interviewing => new[] { InternshipApplicationStatus.Offered, InternshipApplicationStatus.Rejected },
                InternshipApplicationStatus.Offered => new[] { InternshipApplicationStatus.Placed, InternshipApplicationStatus.Rejected },
                InternshipApplicationStatus.PendingAssignment => new[] { InternshipApplicationStatus.Interviewing, InternshipApplicationStatus.Rejected }, // safe fallback
                _ => Array.Empty<InternshipApplicationStatus>()
            };

            if (!allowed.Contains(request.NewStatus))
            {
                _logger.LogWarning("Invalid transition {From} -> {To} for application {AppId}", current, request.NewStatus, request.ApplicationId);
                return Result<UpdateInternshipApplicationStatusResponse>.Failure(_messageService.GetMessage("Application.InvalidTransition"), ResultErrorType.BadRequest);
            }

            // Rejection requires reason
            if (request.NewStatus == InternshipApplicationStatus.Rejected && string.IsNullOrWhiteSpace(request.RejectReason))
            {
                return Result<UpdateInternshipApplicationStatusResponse>.Failure(_messageService.GetMessage("Application.RejectReasonRequired"), ResultErrorType.BadRequest);
            }

            // Apply update
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                application.Status = request.NewStatus;
                application.ReviewedAt = DateTime.UtcNow;
                if (Guid.TryParse(_currentUserService.UserId, out var reviewer))
                {
                    application.ReviewedBy = reviewer;
                }

                if (request.NewStatus == InternshipApplicationStatus.Rejected)
                {
                    application.RejectReason = request.RejectReason?.Trim();
                }

                await _unitOfWork.Repository<InternshipApplication>().UpdateAsync(application, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation("Application {AppId} status changed to {Status} by {UserId}", application.ApplicationId, application.Status, _currentUserService.UserId);

                // Notify student for relevant transitions (Interviewing, Offered, Placed, Rejected)
                var studentUser = application.Student?.User;
                if (studentUser != null && !string.IsNullOrWhiteSpace(studentUser.Email))
                {
                    string subjectKey = request.NewStatus switch
                    {
                        InternshipApplicationStatus.Interviewing => "Job.Application.Notify.Interviewing.Subject",
                        InternshipApplicationStatus.Offered => "Job.Application.Notify.Offered.Subject",
                        InternshipApplicationStatus.Placed => "Job.Application.Notify.Placed.Subject",
                        InternshipApplicationStatus.Rejected => "Job.Application.Notify.Rejected.Subject",
                        _ => null
                    };

                    string bodyKey = request.NewStatus switch
                    {
                        InternshipApplicationStatus.Interviewing => "Job.Application.Notify.Interviewing.Body",
                        InternshipApplicationStatus.Offered => "Job.Application.Notify.Offered.Body",
                        InternshipApplicationStatus.Placed => "Job.Application.Notify.Placed.Body",
                        InternshipApplicationStatus.Rejected => "Job.Application.Notify.Rejected.Body",
                        _ => null
                    };

                    if (subjectKey != null && bodyKey != null)
                    {
                        var subject = _messageService.GetMessage(subjectKey, application.Job?.Title ?? string.Empty);
                        var body = _messageService.GetMessage(bodyKey, application.Job?.Title ?? string.Empty, application.Job?.Enterprise?.Name ?? string.Empty);

                        try
                        {
                            await _emailSender.EnqueueEmailAsync(studentUser.Email, subject, body, application.ApplicationId, application.ReviewedBy, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to enqueue email to student {Email} for application {AppId}", studentUser.Email, application.ApplicationId);
                        }
                    }
                }

                var response = new UpdateInternshipApplicationStatusResponse
                {
                    ApplicationId = application.ApplicationId,
                    Status = application.Status,
                    Message = _messageService.GetMessage("Application.Update.Success")
                };

                return Result<UpdateInternshipApplicationStatusResponse>.Success(response, response.Message);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogWarning(ex, "Concurrency conflict when updating application {AppId}", request.ApplicationId);
                return Result<UpdateInternshipApplicationStatusResponse>.Failure(_messageService.GetMessage("Application.VersionConflict"), ResultErrorType.Conflict);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, "Error while updating application {AppId}", request.ApplicationId);
                return Result<UpdateInternshipApplicationStatusResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.InternalServerError);
            }
        }
    }
}