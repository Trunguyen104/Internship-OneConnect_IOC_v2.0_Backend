using AutoMapper;
using AutoMapper.QueryableExtensions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Extensions.Jobs;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Jobs.Queries.GetJobs
{
    public class GetJobsHandler : MediatR.IRequestHandler<GetJobsQuery, Result<PaginatedResult<GetJobsResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;
        private readonly ILogger<GetJobsHandler> _logger;
        private readonly ICurrentUserService _currentUserService;

        public GetJobsHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IMessageService messageService,
            ILogger<GetJobsHandler> logger,
            ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _messageService = messageService;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        public async Task<Result<PaginatedResult<GetJobsResponse>>> Handle(GetJobsQuery request, CancellationToken cancellationToken)
        {
            var userId = Guid.Parse(_currentUserService.UserId!);

            var jobsQuery = _unitOfWork.Repository<Job>()
                .Query().IgnoreQueryFilters()
                .AsNoTracking()
                .Include(j => j.Enterprise)
                .Include(j => j.Universities)
                .Include(j => j.InternshipApplications)
                .AsQueryable();

            // Enterprise view (HR / EnterpriseAdmin)
            if (JobsPostingParam.GetJobPostings.EnterpriseRoles.Contains(_currentUserService.Role))
            {
                var entUser = await _unitOfWork.Repository<EnterpriseUser>()
                    .Query()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.UserId == userId, cancellationToken);

                if (entUser == null) return Result<PaginatedResult<GetJobsResponse>>.Failure(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.NotAllowed), ResultErrorType.Forbidden);

                jobsQuery = jobsQuery.Where(j => j.EnterpriseId == entUser.EnterpriseId);

                if (request.Status.HasValue) jobsQuery = jobsQuery.Where(j => j.Status == request.Status.Value);
                if (!request.IncludeDeleted) jobsQuery = jobsQuery.Where(j => j.Status != JobStatus.DELETED);
            }
            else // School / Student / other (school-side behavior)
            {
                // Student / school users only see jobs assigned to their university and only published jobs
                var uniUser = await _unitOfWork.Repository<UniversityUser>()
                    .Query()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

                if (uniUser == null) return Result<PaginatedResult<GetJobsResponse>>.Failure(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.NotAllowed), ResultErrorType.Forbidden);

                // If caller is a student and already placed (internship in progress), return message instead of empty page
                var student = await _unitOfWork.Repository<Student>()
                    .Query()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

                if (student != null && student.InternshipStatus == StudentStatus.INTERNSHIP_IN_PROGRESS)
                {
                    // Try to resolve a localized message first; fall back to hard-coded English string.
                    var message = _messageService.GetMessage(MessageKeys.JobPostingMessageKey.InternshipInProgress);
                    return Result<PaginatedResult<GetJobsResponse>>.Failure(message, ResultErrorType.BadRequest);
                }

                // Visibility rules for job audience:
                // - Public: visible to all partner universities (show to all university users)
                // - Targeted: visible only to students of the specific university (job must be linked to that university)
                jobsQuery = jobsQuery.Where(j =>
                    j.Status == JobStatus.PUBLISHED &&
                    (j.Audience == JobAudience.Public || j.Universities.Any(u => u.UniversityId == uniUser.UniversityId))
                );
            }

            // Search
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim();
                jobsQuery = jobsQuery.Where(j => (j.Title ?? string.Empty).Contains(term));
            }

            // Sorting
            var sortOrderDesc = string.Equals(request.SortOrder, "desc", StringComparison.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(request.SortColumn))
            {
                switch (request.SortColumn.ToLowerInvariant())
                {
                    case "title":
                        jobsQuery = sortOrderDesc ? jobsQuery.OrderByDescending(j => j.Title) : jobsQuery.OrderBy(j => j.Title);
                        break;
                    case "expiredate":
                        jobsQuery = sortOrderDesc ? jobsQuery.OrderByDescending(j => j.ExpireDate) : jobsQuery.OrderBy(j => j.ExpireDate);
                        break;
                    default:
                        jobsQuery = jobsQuery.OrderByDescending(j => j.ExpireDate);
                        break;
                }
            }
            else
            {
                jobsQuery = jobsQuery.OrderByDescending(j => j.ExpireDate);
            }

            // Pagination + projection
            var total = await jobsQuery.CountAsync(cancellationToken);
            var items = await jobsQuery
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ProjectTo<GetJobsResponse>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            var result = PaginatedResult<GetJobsResponse>.Create(items, total, request.PageNumber, request.PageSize);
            return Result<PaginatedResult<GetJobsResponse>>.Success(result);
        }
    }
}