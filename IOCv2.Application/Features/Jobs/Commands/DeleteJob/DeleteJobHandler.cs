using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
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
            if (string.IsNullOrWhiteSpace(_currentUserService.UnitId) || !Guid.TryParse(_currentUserService.UnitId, out var enterpriseId))
            {
                return Result<DeleteJobResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.Unauthorized), ResultErrorType.Unauthorized);
            }

            var repo = _unitOfWork.Repository<Job>();

            var job = await repo.Query()
                .Include(j => j.JobApplications)
                .FirstOrDefaultAsync(j => j.JobId == request.JobId, cancellationToken);

            if (job == null)
            {
                return Result<DeleteJobResponse>.NotFound("Job not found");
            }

            // Check ownership: HR can only delete their enterprise jobs
            if (job.EnterpriseId != enterpriseId)
            {
                return Result<DeleteJobResponse>.Failure("You are not allowed to delete this job.", ResultErrorType.Forbidden);
            }

            // Already deleted?
            if (job.DeletedAt.HasValue)
            {
                return Result<DeleteJobResponse>.Failure("Job already deleted.", ResultErrorType.Conflict);
            }

            // Determine active application statuses (Applied / Interview / Offered)
            var activeStatuses = new[] { JobApplicationStatus.Applied, JobApplicationStatus.Interview, JobApplicationStatus.Offered };
            var activeApps = job.JobApplications?.Where(a => activeStatuses.Contains(a.Status)).ToList() ?? new();

            // Case 3: Published or Closed with active applications -> warning unless confirmed
            if ((job.Status == JobStatus.PUBLISHED || job.Status == JobStatus.CLOSED) && activeApps.Any() && !request.ConfirmWhenHasActiveApplications)
            {
                var warning = $"Bài ðãng ðang có [{activeApps.Count}] ?ng viên ðang trong quá tr?nh xét duy?t. Các ?ng viên ? giai ðo?n Interviewing/Offered v?n s? ðý?c ti?p t?c x? l? sau khi xóa. B?n có ch?c mu?n xóa?";
                var preview = new DeleteJobResponse { JobId = job.JobId, DeletedAt = null };
                return Result<DeleteJobResponse>.SuccessWithWarning(preview, warning);
            }

            // Proceed to soft-delete (all cases: Draft, Published/Closed with no active apps, or confirmed)
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                await repo.DeleteAsync(job, cancellationToken); // GenericRepository sets DeletedAt for BaseEntity
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                var response = new DeleteJobResponse
                {
                    JobId = job.JobId,
                    DeletedAt = job.DeletedAt
                };

                var msg = activeApps.Any()
                    ? "Ð? xóa Job Posting. Các ?ng viên ðang x? l? v?n ðý?c ti?p t?c."
                    : "Ð? xóa Job Posting.";

                return Result<DeleteJobResponse>.Success(response, msg);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }
    }
}