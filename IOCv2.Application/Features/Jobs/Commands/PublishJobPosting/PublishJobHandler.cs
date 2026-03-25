using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Jobs.Commands.PublishJob
{
    public class PublishJobHandler : IRequestHandler<PublishJobCommand, Result<PublishJobResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<PublishJobHandler> _logger;

        public PublishJobHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            ILogger<PublishJobHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<Result<PublishJobResponse>> Handle(PublishJobCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Publish job request for JobId {JobId}", request.JobId);

            var repo = _unitOfWork.Repository<Job>();
            var job = await repo.Query().FirstOrDefaultAsync(j => j.JobId == request.JobId, cancellationToken);

            if (job == null)
            {
                return Result<PublishJobResponse>.Failure("Job posting not found.", ResultErrorType.NotFound);
            }

            if (string.IsNullOrWhiteSpace(_currentUserService.UnitId) || !Guid.TryParse(_currentUserService.UnitId, out var enterpriseId))
            {
                return Result<PublishJobResponse>.Failure("Unable to determine enterprise for current user.", ResultErrorType.Unauthorized);
            }

            if (job.EnterpriseId != enterpriseId)
            {
                return Result<PublishJobResponse>.Failure("You are not allowed to publish this job.", ResultErrorType.Forbidden);
            }

            if (job.Status != JobStatus.DRAFT)
            {
                return Result<PublishJobResponse>.Failure("Only Draft job postings can be published.", ResultErrorType.BadRequest);
            }

            // AC-02: Block publish if deadline has passed
            if (job.ExpireDate.HasValue && job.ExpireDate.Value.Date < DateTime.UtcNow.Date)
            {
                return Result<PublishJobResponse>.Failure("Deadline ð? h?t h?n. Vui l?ng c?p nh?t deadline m?i trý?c khi ðãng.", ResultErrorType.BadRequest);
            }

            try
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                job.Status = JobStatus.PUBLISHED;
                job.UpdatedAt = DateTime.UtcNow;

                await repo.UpdateAsync(job, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                var response = new PublishJobResponse
                {
                    JobId = job.JobId,
                    Status = (short)job.Status,
                    UpdatedAt = job.UpdatedAt
                };

                return Result<PublishJobResponse>.Success(response, "Job Posting ð? ðý?c ðãng tuy?n.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while publishing job {JobId}", request.JobId);
                try { await _unitOfWork.RollbackTransactionAsync(cancellationToken); } catch { }
                return Result<PublishJobResponse>.Failure("Internal server error while publishing job.", ResultErrorType.InternalServerError);
            }
        }
    }
}