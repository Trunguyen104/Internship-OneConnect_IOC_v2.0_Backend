using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Extensions.Jobs;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Jobs.Commands.PublishJobPosting
{
    public class PublishJobHandler : IRequestHandler<PublishJobCommand, Result<PublishJobResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<PublishJobHandler> _logger;
        private readonly IMessageService _messageService;

        public PublishJobHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            ILogger<PublishJobHandler> logger,
            IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _logger = logger;
            _messageService = messageService;
        }

        public async Task<Result<PublishJobResponse>> Handle(PublishJobCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var repo = _unitOfWork.Repository<Job>();

                // Read the entity first, validate business rules before starting a transaction.
                var job = await repo.GetByIdAsync(request.JobId, cancellationToken);
                if (job == null)
                    return Result<PublishJobResponse>.Failure(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.JobPostingNotFound), ResultErrorType.NotFound);
                try
                {
                    IsValidJob(job);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Validation failed for job {JobId}: {ErrorMessage}", job?.JobId, ex.Message);
                    return Result<PublishJobResponse>.Failure(ex.Message, ResultErrorType.BadRequest);
                }
                // Start transaction only when ready to modify state.
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                var now = DateTime.UtcNow;
                job.Status = JobStatus.PUBLISHED;
                job.UpdatedAt = now;

                if (Guid.TryParse(_currentUserService.UserId, out var userGuid))
                    job.UpdatedBy = userGuid;

                await repo.UpdateAsync(job, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation("Published job {JobId} by user {UserId}", job.JobId, _currentUserService.UserId);

                var response = new PublishJobResponse
                {
                    JobId = job.JobId,
                    Message = _messageService.GetMessage(MessageKeys.JobPostingMessageKey.PublishSuccess)
                };

                return Result<PublishJobResponse>.Success(response, response.Message);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, "Error while publishing job {JobId}", request.JobId);
                return Result<PublishJobResponse>.Failure(ex.Message, ResultErrorType.InternalServerError);
            }
        }

        private void IsValidJob(Job job)
        {

            if (job == null)
                throw new Exception(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.JobPostingNotFound));

            // Block publish when internship phase not selected
            if (!job.InternshipPhaseId.HasValue || job.InternshipPhaseId == Guid.Empty)
                throw new Exception(_messageService.GetMessage(MessageKeys.InternshipPhase.InternshipPhaseIdRequired));

            if (job.Status == JobStatus.PUBLISHED)
                throw new Exception(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.AlreadyPublished));
            if (string.IsNullOrWhiteSpace(job.Title))
                throw new Exception(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.TitleRequired));
            if (job.Description is not null && job.Description.Length > 4000)
                throw new Exception(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.DescriptionTooLong));
            if (job.Benefit is not null && job.Benefit.Length > 2000)
                throw new Exception(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.BenefitTooLong));
            if (job.Requirements is not null && job.Requirements.Length > 4000)
                throw new Exception(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.RequirementsTooLong));
            if (job.Location is null)
                throw new Exception(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.LocationRequired));
            if (job.Location.Length > 255)
                throw new Exception(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.LocationTooLong));
            if (job.Quantity <= 0)
                throw new Exception(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.QuantityMustBePositive));
            if (!job.ExpireDate.HasValue)
                throw new Exception(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.ExpireDateRequired));
            if (job.ExpireDate.Value <= DateTime.UtcNow)
                throw new Exception(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.ExpireDateMustBeTodayOrLater));
            if (!job.StartDate.HasValue)
                throw new Exception(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.StartDateRequired));
            if (job.StartDate.Value <= DateTime.UtcNow)
                throw new Exception(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.StartDateMustBeTodayOrLater));
            if (!job.EndDate.HasValue)
                throw new Exception(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.EndDateRequired));
            if (job.EndDate.Value.Day < job.StartDate.Value.Day + JobsPostingParam.Common.MinimumDurationDays)
                throw new Exception(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.EndDateMinDuration, JobsPostingParam.Common.MinimumDurationDays));
            if (job.EndDate.Value.Day > job.StartDate.Value.Day + JobsPostingParam.Common.MaximumDurationDays)
                throw new Exception(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.EndDateMaxDuration, JobsPostingParam.Common.MaximumDurationDays));
            if (job.Audience == null)
                throw new Exception(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.AudienceRequired));
        }
    }
}