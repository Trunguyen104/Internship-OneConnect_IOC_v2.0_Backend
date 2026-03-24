using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Jobs.Queries.GetJobById
{
    public class GetJobByIdHandler : MediatR.IRequestHandler<GetJobByIdQuery, Result<GetJobByIdResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private readonly ILogger<GetJobByIdHandler> _logger;
        private readonly ICurrentUserService _currentUserService;

        public GetJobByIdHandler(
            IUnitOfWork unitOfWork,
            IMessageService messageService,
            ILogger<GetJobByIdHandler> logger,
            ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        public async Task<Result<GetJobByIdResponse>> Handle(GetJobByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_currentUserService.UserId) || !Guid.TryParse(_currentUserService.UserId, out var userId))
                {
                    return Result<GetJobByIdResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.Unauthorized), ResultErrorType.Unauthorized);
                }

                // Load the job with enterprise and job applications
                var job = await _unitOfWork.Repository<Job>()
                    .Query()
                    .Include(j => j.Enterprise)
                    .Include(j => j.JobApplications)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(j => j.JobId == request.JobId, cancellationToken);

                if (job == null)
                {
                    return Result<GetJobByIdResponse>.NotFound("Job not found");
                }

                // Load student for current user (to check placed status and existing applications)
                var student = await _unitOfWork.Repository<Student>()
                    .Query()
                    .Include(s => s.JobApplications)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

                // Prepare response
                var resp = new GetJobByIdResponse
                {
                    JobId = job.JobId,
                    Title = job.Title,
                    Description = job.Description,
                    Requirements = job.Requirements,
                    Location = job.Location,
                    ExpireDate = job.ExpireDate,
                    EnterpriseId = job.Enterprise?.EnterpriseId ?? Guid.Empty,
                    EnterpriseName = job.Enterprise?.Name,
                    EnterpriseLogoUrl = job.Enterprise?.LogoUrl,
                    CanApply = false,
                    ApplyDisabledReason = null
                };

                // Default reasons & checks
                var now = DateTime.UtcNow;
                var hasApplied = student != null && student.JobApplications.Any(a => a.JobId == job.JobId);
                var isPlaced = student != null && (student.InternshipStatus == StudentStatus.INTERNSHIP_IN_PROGRESS || student.InternshipStatus == StudentStatus.COMPLETED);
                var jobOpen = job.Status == JobStatus.OPEN;
                var deadlinePassed = job.ExpireDate.HasValue && job.ExpireDate.Value < now;

                if (hasApplied)
                {
                    resp.CanApply = false;
                    resp.ApplyDisabledReason = "Bạn đã ứng tuyển vị trí này";
                }
                else if (isPlaced)
                {
                    resp.CanApply = false;
                    resp.ApplyDisabledReason = "Bạn đã có nơi thực tập";
                }
                else if (!jobOpen)
                {
                    resp.CanApply = false;
                    resp.ApplyDisabledReason = "Vị trí đã đóng";
                }
                else if (deadlinePassed)
                {
                    resp.CanApply = false;
                    resp.ApplyDisabledReason = "Hạn nộp hồ sơ đã hết hạn";
                }
                else
                {
                    resp.CanApply = true;
                }

                return Result<GetJobByIdResponse>.Success(resp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting job {JobId} for user {UserId}", request.JobId, _currentUserService.UserId);
                return Result<GetJobByIdResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.InternalServerError);
            }
        }
    }
}
