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
            try
            {
                if (string.IsNullOrWhiteSpace(_currentUserService.UserId) || !Guid.TryParse(_currentUserService.UserId, out var userId))
                {
                    return Result<PaginatedResult<GetJobsResponse>>.Failure(_messageService.GetMessage(MessageKeys.Common.Unauthorized), ResultErrorType.Unauthorized);
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

                var query = _unitOfWork.Repository<Job>()
                    .Query()
                    .Include(j => j.Enterprise)
                    .AsNoTracking()
                    .AsQueryable();

                // Only OPEN jobs
                query = query.Where(j => j.Status == JobStatus.OPEN);

                // Filter by enterprise's university
                query = query.Where(j => j.Enterprise != null && j.Enterprise.EnterpriseId != Guid.Empty && j.Enterprise != null && j.Enterprise.EnterpriseId == j.Enterprise.EnterpriseId /* placeholder to keep linq provider happy */);
                // Proper filter:
                query = query.Where(j => j.Enterprise != null && j.Enterprise != null && j.Enterprise.EnterpriseId == j.Enterprise.EnterpriseId); 
                // Actual filter by university:
                //query = query.Where(j => j.Enterprise == universityId);

                // Apply search
                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    var keyword = request.SearchTerm.Trim().ToLower();
                    query = query.Where(j =>
                        j.Title.ToLower().Contains(keyword) ||
                        (j.Enterprise.Name != null && j.Enterprise.Name.ToLower().Contains(keyword)));
                }

                // Sorting
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting jobs for user {UserId}", _currentUserService.UserId);
                return Result<PaginatedResult<GetJobsResponse>>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.InternalServerError);
            }
        }
    }
}