using AutoMapper;
using AutoMapper.QueryableExtensions;
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
            if (string.IsNullOrWhiteSpace(_currentUserService.UserId) || !Guid.TryParse(_currentUserService.UserId, out var userId))
            {
                return Result<PaginatedResult<GetJobsResponse>>.Failure(_messageService.GetMessage(MessageKeys.Common.Unauthorized), ResultErrorType.Unauthorized);
            }

            // If the current user is an Enterprise/HR account, return jobs for that enterprise
            var enterpriseUser = await _unitOfWork.Repository<EnterpriseUser>()
                .Query()
                .FirstOrDefaultAsync(e => e.UserId == userId, cancellationToken);

            if (enterpriseUser != null)
            {
                var query = _unitOfWork.Repository<Job>()
                    .Query()
                    .Include(j => j.Enterprise)
                    .AsNoTracking()
                    // only jobs belonging to this enterprise and not marked deleted
                    .Where(j => j.Enterprise != null && j.Enterprise.EnterpriseId == enterpriseUser.EnterpriseId && j.Status != JobStatus.DELETED)
                    .AsQueryable();

                // Optional status filter for HR view (Draft / Published / Closed)
                if (request.Status.HasValue)
                {
                    query = query.Where(j => j.Status == request.Status.Value);
                }

                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    var keyword = request.SearchTerm.Trim().ToLower();
                    query = query.Where(j =>
                        j.Title.ToLower().Contains(keyword) ||
                        (j.Enterprise.Name != null && j.Enterprise.Name.ToLower().Contains(keyword)));
                }

                if (string.IsNullOrWhiteSpace(request.SortColumn))
                {
                    query = query.OrderByDescending(j => j.CreatedAt);
                }
                else
                {
                    var isDesc = request.SortOrder?.ToLower() == "desc";
                    query = request.SortColumn?.ToLower() switch
                    {
                        "title" => isDesc ? query.OrderByDescending(j => j.Title) : query.OrderBy(j => j.Title),
                        "expiredate" => isDesc ? query.OrderByDescending(j => j.ExpireDate) : query.OrderBy(j => j.ExpireDate),
                        _ => query.OrderByDescending(j => j.CreatedAt)
                    };
                }

                var totalCount = await query.CountAsync(cancellationToken);
                var items = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ProjectTo<GetJobsResponse>(_mapper.ConfigurationProvider)
                    .ToListAsync(cancellationToken);

                var result = PaginatedResult<GetJobsResponse>.Create(items, totalCount, request.PageNumber, request.PageSize);
                return Result<PaginatedResult<GetJobsResponse>>.Success(result);
            }

            // Load student (and their terms) for current user
            var student = await _unitOfWork.Repository<Student>()
                .Query()
                .Include(s => s.StudentTerms)
                    .ThenInclude(st => st.Term)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

            if (student == null)
            {
                return Result<PaginatedResult<GetJobsResponse>>.Failure(_messageService.GetMessage(MessageKeys.Users.NotFound), ResultErrorType.NotFound);
            }

            // If student already placed (considered "Placed" when internship in progress or completed) -> hide list
            if (student.InternshipStatus == StudentStatus.INTERNSHIP_IN_PROGRESS ||
                student.InternshipStatus == StudentStatus.COMPLETED)
            {
                // Return Forbidden with user-friendly message so client can show "Bạn đã có nơi thực tập"
                return Result<PaginatedResult<GetJobsResponse>>.Failure("Bạn đã có nơi thực tập", ResultErrorType.Forbidden);
            }

            // Determine the student's active term and its university
            var activeTerm = student.StudentTerms
                .Select(st => st.Term)
                .FirstOrDefault(t => t.Status == TermStatus.Open);

            if (activeTerm == null)
            {
                // No active term -> return empty list
                var empty = PaginatedResult<GetJobsResponse>.Create(Enumerable.Empty<GetJobsResponse>().ToList(), 0, request.PageNumber, request.PageSize);
                return Result<PaginatedResult<GetJobsResponse>>.Success(empty);
            }

            var universityId = activeTerm.UniversityId;

            var stuQuery = _unitOfWork.Repository<Job>()
                .Query()
                .Include(j => j.Enterprise).ThenInclude(e => e.InternshipApplications).ThenInclude(e => e.Term)
                .AsNoTracking()
                .AsQueryable();

            // Only OPEN jobs for students (Published)
            stuQuery = stuQuery.Where(j => j.Status == JobStatus.PUBLISHED);

            // Filter by university via internship applications' term -> university
            stuQuery = stuQuery.Where(j => j.Enterprise != null && j.Enterprise.InternshipApplications.Any(ia => ia.Term != null && ia.Term.UniversityId == universityId));

            // Apply search
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var keyword = request.SearchTerm.Trim().ToLower();
                stuQuery = stuQuery.Where(j =>
                    j.Title.ToLower().Contains(keyword) ||
                    (j.Enterprise.Name != null && j.Enterprise.Name.ToLower().Contains(keyword)));
            }

            // Sorting
            if (string.IsNullOrWhiteSpace(request.SortColumn))
            {
                stuQuery = stuQuery.OrderByDescending(j => j.CreatedAt);
            }
            else
            {
                var isDesc = request.SortOrder?.ToLower() == "desc";
                stuQuery = request.SortColumn?.ToLower() switch
                {
                    "title" => isDesc ? stuQuery.OrderByDescending(j => j.Title) : stuQuery.OrderBy(j => j.Title),
                    "expiredate" => isDesc ? stuQuery.OrderByDescending(j => j.ExpireDate) : stuQuery.OrderBy(j => j.ExpireDate),
                    _ => stuQuery.OrderByDescending(j => j.CreatedAt)
                };
            }

            var stuTotal = await stuQuery.CountAsync(cancellationToken);
            var stuItems = await stuQuery
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ProjectTo<GetJobsResponse>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            var stuResult = PaginatedResult<GetJobsResponse>.Create(stuItems, stuTotal, request.PageNumber, request.PageSize);
            return Result<PaginatedResult<GetJobsResponse>>.Success(stuResult);
        }
    }
}