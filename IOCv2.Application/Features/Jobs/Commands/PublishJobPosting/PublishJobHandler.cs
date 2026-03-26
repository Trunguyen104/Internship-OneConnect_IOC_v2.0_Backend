using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
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
            try
            {
                var repo = _unitOfWork.Repository<Job>();
                await _unitOfWork.BeginTransactionAsync(cancellationToken);
                var job = await repo.GetByIdAsync(request.JobId, cancellationToken);

                if (job == null)
                    return Result<PublishJobResponse>.NotFound("Job Posting không tồn tại.");

                // Defensive checks in handler as well (validator should enforce these)
                if (job.Status != JobStatus.DRAFT)
                    return Result<PublishJobResponse>.Failure("Job Posting không ở trạng thái Draft.");

                if (job.ExpireDate.HasValue && job.ExpireDate.Value < DateTime.UtcNow)
                    return Result<PublishJobResponse>.Failure("Deadline đã hết hạn. Vui lòng cập nhật deadline mới trước khi đăng.");

                job.Status = JobStatus.PUBLISHED;
                job.UpdatedAt = DateTime.UtcNow;

                if (Guid.TryParse(_currentUserService.UserId, out var userGuid))
                    job.UpdatedBy = userGuid;
                
                await repo.UpdateAsync(job, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                var response = new PublishJobResponse
                {
                    JobId = job.JobId,
                    Message = "Job Posting đã được đăng tuyển."
                };

                return Result<PublishJobResponse>.Success(response, response.Message);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, "Error while publishing job {JobId}", request.JobId);
                return Result<PublishJobResponse>.Failure("Có lỗi xảy ra khi đăng tuyển. Vui lòng thử lại sau.", ResultErrorType.InternalServerError);
            }
        }
    }
}