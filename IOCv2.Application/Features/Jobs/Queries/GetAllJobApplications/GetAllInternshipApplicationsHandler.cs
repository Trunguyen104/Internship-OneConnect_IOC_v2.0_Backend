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

namespace IOCv2.Application.Features.Jobs.Queries.GetAllJobApplications
{
    public class GetAllInternshipApplicationsHandler : MediatR.IRequestHandler<GetAllInternshipApplicationsQuery, Result<PaginatedResult<GetAllInternshipApplicationResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;
        private readonly ILogger<GetAllInternshipApplicationsHandler> _logger;
        private readonly ICurrentUserService _currentUserService;

        public GetAllInternshipApplicationsHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IMessageService messageService,
            ILogger<GetAllInternshipApplicationsHandler> logger,
            ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _messageService = messageService;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        public async Task<Result<PaginatedResult<GetAllInternshipApplicationResponse>>> Handle(GetAllInternshipApplicationsQuery request, CancellationToken cancellationToken)
        {

            if (string.IsNullOrWhiteSpace(_currentUserService.UnitId) || !Guid.TryParse(_currentUserService.UnitId, out var enterpriseId))
            {
                return Result<PaginatedResult<GetAllInternshipApplicationResponse>>.Failure(_messageService.GetMessage(MessageKeys.Common.Unauthorized), ResultErrorType.Unauthorized);
            }

            // Only HR for an enterprise should see its applications
            var query = _unitOfWork.Repository<InternshipApplication>()
                .Query()
                .Include(ja => ja.Job)
                .ThenInclude(j => j.Enterprise)
                .Include(ja => ja.Student).ThenInclude(s => s.User)
                .AsNoTracking()
                .Where(ja => ja.Job.EnterpriseId == enterpriseId)
                .AsQueryable();

            // Filter by job id (include closed/deleted; Job may be Deleted if DeletedAt != null)
            if (request.JobId.HasValue && request.JobId.Value != Guid.Empty)
            {
                query = query.Where(ja => ja.JobId == request.JobId.Value);
            }

            // Optional filter by job status
            if (request.JobStatus.HasValue)
            {
                query = query.Where(ja => (short)ja.Job.Status == (short)request.JobStatus.Value);
            }

            // Filter by application status
            if (request.ApplicationStatus.HasValue)
            {
                var s = request.ApplicationStatus.Value;
                query = query.Where(ja => ja.Status == s);
            }

            // Search on student name/email or job title
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var k = request.SearchTerm.Trim().ToLower();
                query = query.Where(ja =>
                    (ja.Student.User.FullName != null && ja.Student.User.FullName.ToLower().Contains(k)) ||
                    (ja.Student.User.Email != null && ja.Student.User.Email.ToLower().Contains(k)) ||
                    (ja.Job.Title != null && ja.Job.Title.ToLower().Contains(k)));
            }

            // Sorting
            var isDesc = string.Equals(request.SortOrder, "desc", StringComparison.OrdinalIgnoreCase);
            query = request.SortColumn?.ToLower() switch
            {
                "student" => isDesc ? query.OrderByDescending(ja => ja.Student.User.FullName) : query.OrderBy(ja => ja.Student.User.FullName),
                "status" => isDesc ? query.OrderByDescending(ja => ja.Status) : query.OrderBy(ja => ja.Status),
                "appliedat" => isDesc ? query.OrderByDescending(ja => ja.AppliedAt) : query.OrderBy(ja => ja.AppliedAt),
                _ => isDesc ? query.OrderByDescending(ja => ja.AppliedAt) : query.OrderBy(ja => ja.AppliedAt)
            };

            if (request.PageNumber <= 0)
            {
                _logger.LogWarning("Invalid PageNumber {PageNumber} provided, defaulting to 1.", request.PageNumber);
                request.PageNumber = 1;
            }

            if (request.PageSize <= 0)
            {
                _logger.LogWarning("Invalid PageSize {PageSize} provided, defaulting to 10.", request.PageSize);
                request.PageSize = 10; // or choose a project-default constant
            }

            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ProjectTo<GetAllInternshipApplicationResponse>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            var result = PaginatedResult<GetAllInternshipApplicationResponse>.Create(items, total, request.PageNumber, request.PageSize);
            return Result<PaginatedResult<GetAllInternshipApplicationResponse>>.Success(result);

        }
    }
}