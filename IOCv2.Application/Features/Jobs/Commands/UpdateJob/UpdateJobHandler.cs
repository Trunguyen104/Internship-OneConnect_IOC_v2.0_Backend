using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace IOCv2.Application.Features.Jobs.Commands.UpdateJob
{
    public class UpdateJobHandler : IRequestHandler<UpdateJobCommand, Result<UpdateJobResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<UpdateJobHandler> _logger;

        public UpdateJobHandler(
            IUnitOfWork unitOfWork,
            IMessageService messageService,
            ICurrentUserService currentUserService,
            ILogger<UpdateJobHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<Result<UpdateJobResponse>> Handle(UpdateJobCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Update job request {JobId} by User {UserId}", request.JobId, _currentUserService.UserId);

            if (string.IsNullOrWhiteSpace(_currentUserService.UnitId) || !Guid.TryParse(_currentUserService.UnitId, out var enterpriseId))
            {
                return Result<UpdateJobResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.Unauthorized), ResultErrorType.Unauthorized);
            }

            var repo = _unitOfWork.Repository<Job>();
            var job = await repo.Query()
                .Include(j => j.JobApplications)
                .FirstOrDefaultAsync(j => j.JobId == request.JobId, cancellationToken);

            if (job == null)
            {
                return Result<UpdateJobResponse>.NotFound("Job not found");
            }

            if (job.EnterpriseId != enterpriseId)
            {
                return Result<UpdateJobResponse>.Failure("You are not allowed to edit this job.", ResultErrorType.Forbidden);
            }

            // Basic validation (matches AC-01 expectations)
            if (!string.IsNullOrWhiteSpace(request.Title) && string.IsNullOrWhiteSpace(request.Title.Trim()))
            {
                return Result<UpdateJobResponse>.Failure("Title is required.", ResultErrorType.BadRequest);
            }

            if (request.ExpireDate.HasValue && request.ExpireDate.Value.Date < DateTime.UtcNow.Date)
            {
                return Result<UpdateJobResponse>.Failure("ExpireDate must be today or in the future.", ResultErrorType.BadRequest);
            }

            // AC-05 logic
            var hasApplications = job.JobApplications != null && job.JobApplications.Any();
            if (job.Status == JobStatus.PUBLISHED && hasApplications && !request.ConfirmWhenHasApplications)
            {
                // Return a success-with-warning (UI should prompt HR to confirm). Do NOT mutate DB.
                var warning = $"Bài đăng này đang có [{job.JobApplications.Count}] ứng viên. Thay đổi thông tin có thể ảnh hưởng đến kỳ vọng của ứng viên. Bạn có chắc muốn tiếp tục?";
                var preview = new UpdateJobResponse
                {
                    JobId = job.JobId,
                    Status = (short)job.Status,
                    UpdatedAt = job.UpdatedAt
                };

                return Result<UpdateJobResponse>.SuccessWithWarning(preview, warning);
            }

            // If Closed -> reopen to Published after validation; require new valid deadline per AC note
            if (job.Status == JobStatus.CLOSED)
            {
                // Require a new expire date to reopen
                if (!request.ExpireDate.HasValue || request.ExpireDate.Value.Date < DateTime.UtcNow.Date)
                {
                    return Result<UpdateJobResponse>.Failure("To reopen a Closed job you must provide a valid future deadline.", ResultErrorType.BadRequest);
                }

                job.Status = JobStatus.PUBLISHED;
            }

            // Apply updates (for Draft, Published after confirm, Closed after valid reopen)
            try
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                if (request.Title is not null) job.Title = request.Title;
                if (request.Description is not null) job.Description = request.Description;
                if (request.Requirements is not null) job.Requirements = request.Requirements;
                if (request.Location is not null) job.Location = request.Location;
                if (request.Quantity.HasValue) job.Quantity = request.Quantity;
                if (request.ExpireDate.HasValue) job.ExpireDate = request.ExpireDate;

                job.UpdatedAt = DateTime.UtcNow;

                await repo.UpdateAsync(job, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                var response = new UpdateJobResponse
                {
                    JobId = job.JobId,
                    Status = (short)job.Status,
                    UpdatedAt = job.UpdatedAt
                };

                return Result<UpdateJobResponse>.Success(response, "Đã thay đổi Job Posting.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating job {JobId}", job.JobId);
                try { await _unitOfWork.RollbackTransactionAsync(cancellationToken); } catch { }
                return Result<UpdateJobResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.InternalServerError);
            }
        }
    }
}
