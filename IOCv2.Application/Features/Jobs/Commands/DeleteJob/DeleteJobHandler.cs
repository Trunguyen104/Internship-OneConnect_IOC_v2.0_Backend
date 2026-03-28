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

namespace IOCv2.Application.Features.Jobs.Commands.DeleteJob
{
    public class DeleteJobHandler : IRequestHandler<DeleteJobCommand, Result<DeleteJobResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<DeleteJobHandler> _logger;

        public DeleteJobHandler(
            IUnitOfWork unitOfWork,
            IMessageService messageService,
            ICurrentUserService currentUserService,
            ILogger<DeleteJobHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<Result<DeleteJobResponse>> Handle(DeleteJobCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Delete job requested: {JobId} by {UserId}", request.JobId, _currentUserService.UserId);

            var job = await _unitOfWork.Repository<Job>()
                .Query()
                .Include(j => j.InternshipApplications)
                .FirstOrDefaultAsync(j => j.JobId == request.JobId, cancellationToken);

            if (job == null)
            {
                _logger.LogWarning("Job not found: {JobId}", request.JobId);
                return Result<DeleteJobResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.RecordNotFound), ResultErrorType.NotFound);
            }

            // Security: only HR of owning enterprise can delete
            if (JobsPostingParam.GetJobPostings.EnterpriseRoles.Contains(_currentUserService.Role))
            {
                if (!Guid.TryParse(_currentUserService.UserId, out var userId))
                {
                    return Result<DeleteJobResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.Unauthorized), ResultErrorType.Unauthorized);
                }

                var entUser = await _unitOfWork.Repository<EnterpriseUser>()
                    .Query()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.UserId == userId, cancellationToken);

                if (entUser == null || entUser.EnterpriseId != job.EnterpriseId)
                {
                    return Result<DeleteJobResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.Forbidden), ResultErrorType.Forbidden);
                }
            }
            else
            {
                return Result<DeleteJobResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.Forbidden), ResultErrorType.Forbidden);
            }

            // Only proceed for non-deleted jobs
            if (job.Status == JobStatus.DELETED)
            {
                var already = _messageService.GetMessage(MessageKeys.JobPostingMessageKey.AlreadyDeleted);
                return Result<DeleteJobResponse>.Failure(already, ResultErrorType.BadRequest);
            }

            // Active statuses per AC-08: PendingHRApproval (Applied), Interviewing, Offered
            var activeStatuses = new[]
            {
                InternshipApplicationStatus.Applied,
                InternshipApplicationStatus.Interviewing,
                InternshipApplicationStatus.Offered
            };

            var activeApplications = job.InternshipApplications?
                .Where(a => activeStatuses.Contains(a.Status))
                .ToList() ?? new();

            var activeCount = activeApplications.Count;

            // If there are active applications and caller didn't confirm, return confirmation warning
            if (activeCount > 0 && !request.ConfirmWhenHasActiveApplications)
            {
                var confirmMsg = _messageService.GetMessage(MessageKeys.JobPostingMessageKey.DeleteConfirmHasActiveApplications, activeCount);
                // Use Conflict so frontend can prompt user and call again with ConfirmWhenHasActiveApplications = true
                return Result<DeleteJobResponse>.Failure(confirmMsg, ResultErrorType.Conflict);
            }

            // Proceed to soft-delete (mark as Deleted)
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                job.Status = JobStatus.DELETED;
                job.DeletedAt = DateTime.UtcNow;
                job.UpdatedAt = DateTime.UtcNow;
                if (Guid.TryParse(_currentUserService.UserId, out var updBy))
                {
                    job.UpdatedBy = updBy;
                }

                await _unitOfWork.Repository<Job>().UpdateAsync(job, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation("Job {JobId} soft-deleted by {UserId}. ActiveApplications: {Count}", job.JobId, _currentUserService.UserId, activeCount);

                // Per AC-08: do NOT modify applications (they remain as-is); no university notify.
                var successKey = activeCount > 0 ? MessageKeys.JobPostingMessageKey.DeleteWithActiveApplications : MessageKeys.JobPostingMessageKey.DeleteSuccess;
                var message = _messageService.GetMessage(successKey, job.Title ?? string.Empty);
                var response = new DeleteJobResponse { Message = message };
                return Result<DeleteJobResponse>.Success(response, message);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogWarning(ex, "Concurrency conflict when deleting job {JobId}", request.JobId);
                return Result<DeleteJobResponse>.Failure(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.DleteVersionConflict), ResultErrorType.Conflict);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, "Error while deleting job {JobId}", request.JobId);
                return Result<DeleteJobResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.InternalServerError);
            }
        }
    }
}