using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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

                // Default reasons & checks (student apply logic)
                var now = DateTime.UtcNow;
                var hasApplied = student != null && student.JobApplications.Any(a => a.JobId == job.JobId);
                var isPlaced = student != null && (student.InternshipStatus == StudentStatus.INTERNSHIP_IN_PROGRESS || student.InternshipStatus == StudentStatus.COMPLETED);
                var jobOpen = job.Status == JobStatus.PUBLISHED;
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

                // Compute application counts per status
                var appCounts = job.JobApplications
                    .GroupBy(a => a.Status)
                    .Select(g => new ApplicationStatusCountDto
                    {
                        Status = (short)g.Key,
                        StatusName = g.Key.ToString(),
                        Count = g.Count()
                    })
                    .ToList();

                // Ensure zero counts for statuses that are missing (optional)
                foreach (JobApplicationStatus status in Enum.GetValues(typeof(JobApplicationStatus)))
                {
                    if (!appCounts.Any(c => c.Status == (short)status))
                    {
                        appCounts.Add(new ApplicationStatusCountDto
                        {
                            Status = (short)status,
                            StatusName = status.ToString(),
                            Count = 0
                        });
                    }
                }

                // Sort by enum value for stable ordering
                resp.ApplicationCounts = appCounts.OrderBy(c => c.Status).ToList();

                // Determine allowed actions for current user
                var allowedActions = new List<string>();

                // Is current user HR associated with this enterprise?
                var enterpriseUser = await _unitOfWork.Repository<EnterpriseUser>()
                    .Query()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(eu => eu.UserId == userId && eu.EnterpriseId == job.EnterpriseId, cancellationToken);

                if (enterpriseUser != null)
                {
                    // HR actions based on job.Status
                    switch (job.Status)
                    {
                        case JobStatus.DRAFT:
                            allowedActions.AddRange(new[] { "Edit", "Publish", "Delete", "ViewApplications" });
                            break;
                        case JobStatus.PUBLISHED:
                            allowedActions.AddRange(new[] { "Edit", "Close", "ViewApplications" });
                            break;
                        case JobStatus.CLOSED:
                            allowedActions.AddRange(new[] { "Edit", "Reopen", "Delete", "ViewApplications" });
                            break;
                        case JobStatus.DELETED:
                            allowedActions.AddRange(new[] { "Restore" });
                            break;
                        default:
                            allowedActions.Add("ViewApplications");
                            break;
                    }
                }
                else
                {
                    // Non-HR users (student / others): include Apply if allowed and always allow viewing basic details
                    if (resp.CanApply)
                    {
                        allowedActions.Add("Apply");
                    }
                }

                resp.AllowedActions = allowedActions;

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
