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
            if (string.IsNullOrWhiteSpace(_currentUserService.UnitId) || !Guid.TryParse(_currentUserService.UnitId, out var enterpriseId))
            {
                return Result<CloseJobResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.Unauthorized), ResultErrorType.Unauthorized);
            }

            var repo = _unitOfWork.Repository<Job>();

            // Load job + enterprise + job applications -> include student -> include user for emails
            var job = await repo.Query()
                .Include(j => j.Enterprise).ThenInclude(e => e.EnterpriseUsers).ThenInclude(eu => eu.User)
                .Include(j => j.JobApplications).ThenInclude(ja => ja.Student).ThenInclude(s => s.User)
                .FirstOrDefaultAsync(j => j.JobId == request.JobId, cancellationToken);

            if (job == null)
                return Result<CloseJobResponse>.NotFound("Job not found");

            if (job.EnterpriseId != enterpriseId)
                return Result<CloseJobResponse>.Failure("You are not allowed to close this job.", ResultErrorType.Forbidden);

            if (job.Status != JobStatus.PUBLISHED)
            {
                return Result<CloseJobResponse>.Failure("Only Published job postings can be closed.", ResultErrorType.BadRequest);
            }

            // Active application statuses per AC: Applied / Interview / Offered
            var activeStatuses = new[] { JobApplicationStatus.Applied, JobApplicationStatus.Interview, JobApplicationStatus.Offered };
            var activeApps = job.JobApplications?.Where(a => activeStatuses.Contains(a.Status)).ToList() ?? new();

            if (activeApps.Any() && !request.ConfirmWhenHasActiveApplications)
            {
                var warning = $"Bài đăng đang có [{activeApps.Count}] ứng viên đang trong quá trình xét duyệt. Sau khi đóng, sinh viên mới sẽ không thể ứng tuyển thêm, nhưng các ứng viên hiện tại vẫn được tiếp tục xử lý. Bạn có chắc muốn đóng?";
                // return success-with-warning so UI can ask for confirmation; no DB change.
                var preview = new CloseJobResponse { JobId = job.JobId, Status = (short)job.Status, UpdatedAt = job.UpdatedAt };
                return Result<CloseJobResponse>.SuccessWithWarning(preview, warning);
            }

            // Proceed to close
            try
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                job.Status = JobStatus.CLOSED;
                job.UpdatedAt = DateTime.UtcNow;

                await repo.UpdateAsync(job, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                // Notify affected students (only those in active statuses)
                foreach (var app in activeApps)
                {
                    var studentUser = app.Student?.User;
                    if (studentUser != null && !string.IsNullOrWhiteSpace(studentUser.Email))
                    {
                        var subj = $"Job Posting \"{job.Title}\" đã được đóng";
                        var body = $"Job Posting \"{job.Title}\" đã được đóng. Hồ sơ của bạn vẫn sẽ được {job.Enterprise?.Name} tiếp tục xem xét nếu bạn đang trong quá trình phỏng vấn/offer.";
                        // best-effort enqueue background email; don't await extensively
                        _ = _emailSender.EnqueueEmailAsync(studentUser.Email, subj, body, job.JobId, null, cancellationToken);
                    }
                }

                // Return success
                var response = new CloseJobResponse
                {
                    JobId = job.JobId,
                    Status = (short)job.Status,
                    UpdatedAt = job.UpdatedAt
                };

                return Result<CloseJobResponse>.Success(response, "Đã đóng Job Posting.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing job {JobId}", job.JobId);
                try { await _unitOfWork.RollbackTransactionAsync(cancellationToken); } catch { }
                return Result<CloseJobResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.InternalServerError);
            }
        }
    }
}
