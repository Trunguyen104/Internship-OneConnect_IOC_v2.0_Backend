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

namespace IOCv2.Application.Features.Jobs.Commands.CloseJob
{
    public class CloseJobHandler : IRequestHandler<CloseJobCommand, Result<CloseJobResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private readonly IBackgroundEmailSender _emailSender;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<CloseJobHandler> _logger;

        public CloseJobHandler(
            IUnitOfWork unitOfWork,
            IMessageService messageService,
            IBackgroundEmailSender emailSender,
            ICurrentUserService currentUserService,
            ILogger<CloseJobHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _emailSender = emailSender;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<Result<CloseJobResponse>> Handle(CloseJobCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Close job requested: {JobId} by {UserId}", request.JobId, _currentUserService.UserId);

            // Load job + related applications + students + user + enterprise
            var job = await _unitOfWork.Repository<Job>()
                .Query()
                .AsNoTracking()
                .Include(j => j.Enterprise)
                .Include(j => j.InternshipApplications)
                    .ThenInclude(a => a.Student)
                        .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(j => j.JobId == request.JobId, cancellationToken);

            if (job == null)
            {
                _logger.LogWarning("Job not found: {JobId}", request.JobId);
                return Result<CloseJobResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.RecordNotFound), ResultErrorType.NotFound);
            }

            // Only HR of owning enterprise may close (enterprise roles)
            if (JobsPostingParam.GetJobPostings.EnterpriseRoles.Contains(_currentUserService.Role))
            {
                if (!Guid.TryParse(_currentUserService.UserId, out var userId))
                {
                    return Result<CloseJobResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.Unauthorized), ResultErrorType.Unauthorized);
                }

                var entUser = await _unitOfWork.Repository<EnterpriseUser>()
                    .Query()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.UserId == userId, cancellationToken);

                if (entUser == null || entUser.EnterpriseId != job.EnterpriseId)
                {
                    return Result<CloseJobResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.Forbidden), ResultErrorType.Forbidden);
                }
            }
            else
            {
                // Non-enterprise users cannot close jobs
                return Result<CloseJobResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.Forbidden), ResultErrorType.Forbidden);
            }

            // Only Published jobs should be closed in AC-06
            if (job.Status != JobStatus.PUBLISHED)
            {
                var msg = _messageService.GetMessage(MessageKeys.JobPostingMessageKey.OnlyPublishedAllowed);
                return Result<CloseJobResponse>.Failure(msg, ResultErrorType.BadRequest);
            }

            // Active application statuses per AC-06: PendingHRApproval (Applied), Interviewing, Offered
            var activeStatuses = new[] {
                InternshipApplicationStatus.Applied,
                InternshipApplicationStatus.Interviewing,
                InternshipApplicationStatus.Offered
            };

            var activeApplications = job.InternshipApplications?
                .Where(a => activeStatuses.Contains(a.Status))
                .ToList() ?? new();

            var activeCount = activeApplications.Count;

            if (activeCount > 0 && !request.ConfirmWhenHasActiveApplications)
            {
                // Ask frontend to confirm closing despite active applications
                var confirmMsg = _messageService.GetMessage(MessageKeys.JobPostingMessageKey.ConfirmHasActiveApplications, activeCount);
                return Result<CloseJobResponse>.Failure(confirmMsg, ResultErrorType.Conflict);
            }

            // Proceed to close
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                // change status
                job.Status = JobStatus.CLOSED;
                job.UpdatedAt = DateTime.UtcNow;
                if (Guid.TryParse(_currentUserService.UserId, out var updBy))
                {
                    job.UpdatedBy = updBy;
                }

                await _unitOfWork.Repository<Job>().UpdateAsync(job, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation("Job {JobId} closed by {UserId}. ActiveApplications: {Count}", job.JobId, _currentUserService.UserId, activeCount);

                // Notify students with active applications
                if (activeCount > 0)
                {
                    var subject = _messageService.GetMessage(MessageKeys.JobPostingMessageKey.NotifyStudentClosedSubject, job.Title);
                    var bodyTemplate = _messageService.GetMessage(MessageKeys.JobPostingMessageKey.NotifyStudentClosedBody, job.Title, job.Enterprise?.Name ?? string.Empty);

                    // Send email to each distinct student (by email)
                    var distinctUsers = activeApplications
                        .Select(a => a.Student?.User)
                        .Where(u => u != null)
                        .GroupBy(u => u!.Email.ToLowerInvariant())
                        .Select(g => g.First()!)
                        .ToList();

                    foreach (var user in distinctUsers)
                    {
                        try
                        {
                            // Enqueue background email (do not fail the operation if email enqueue fails)
                            await _emailSender.EnqueueEmailAsync(user.Email, subject, bodyTemplate, job.JobId, job.UpdatedBy, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed enqueueing email notification for user {Email} about closed job {JobId}", user.Email, job.JobId);
                        }
                    }
                }

                var successMsg = _messageService.GetMessage(MessageKeys.JobPostingMessageKey.CloseSuccess, job.Title);
                var response = new CloseJobResponse { Message = successMsg };
                return Result<CloseJobResponse>.Success(response, successMsg);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, "Error while closing job {JobId}", request.JobId);
                return Result<CloseJobResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.InternalServerError);
            }
        }
    }
}
