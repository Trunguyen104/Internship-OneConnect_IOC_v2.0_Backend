using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.InternshipGroups.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.InternshipGroups.Queries.GetInternshipGroups
{
    public class GetInternshipGroupsHandler : IRequestHandler<GetInternshipGroupsQuery, Result<PaginatedResult<GetInternshipGroupsResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetInternshipGroupsHandler> _logger;

        public GetInternshipGroupsHandler(IUnitOfWork unitOfWork, IMapper mapper, ICacheService cacheService, ILogger<GetInternshipGroupsHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<Result<PaginatedResult<GetInternshipGroupsResponse>>> Handle(GetInternshipGroupsQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = InternshipGroupCacheKeys.GroupList(
                request.PageNumber,
                request.PageSize,
                request.SearchTerm,
                request.Status.HasValue ? (int)request.Status.Value : null,
                request.TermId,
                request.IncludeArchived,
                request.EnterpriseId);

            var cached = await _cacheService.GetAsync<PaginatedResult<GetInternshipGroupsResponse>>(cacheKey, cancellationToken);
            if (cached != null)
            {
                return Result<PaginatedResult<GetInternshipGroupsResponse>>.Success(cached);
            }

            var query = _unitOfWork.Repository<InternshipGroup>().Query()
                .Include(ig => ig.Enterprise)
                .Include(ig => ig.Mentor!).ThenInclude(m => m.User!)
                .Include(ig => ig.Members)
                .Include(ig => ig.Term)
                .AsNoTracking();

            // Lọc theo GroupName hoặc EnterpriseName nếu có SearchTerm
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                var lowerSearch = request.SearchTerm.ToLower();
                query = query.Where(x => x.GroupName.ToLower().Contains(lowerSearch) ||
                                        (x.Enterprise != null && x.Enterprise.Name.ToLower().Contains(lowerSearch)));
            }

            // Lọc theo Status & IncludeArchived
            if (request.Status.HasValue)
            {
                query = query.Where(x => x.Status == request.Status.Value);
            }
            else if (!request.IncludeArchived)
            {
                query = query.Where(x => x.Status != GroupStatus.Archived);
            }

            // Lọc theo TermId
            if (request.TermId.HasValue)
            {
                query = query.Where(x => x.TermId == request.TermId.Value);
            }

            // Lọc theo EnterpriseId
            if (request.EnterpriseId.HasValue)
            {
                query = query.Where(x => x.EnterpriseId == request.EnterpriseId.Value);
            }

            // Sắp xếp mặc định theo ngày bắt đầu giảm dần hoặc tên
            query = query.OrderByDescending(x => x.CreatedAt);

            // Xử lý Pagination
            var totalCount = await query.CountAsync(cancellationToken);

            var entities = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var resultItems = _mapper.Map<List<GetInternshipGroupsResponse>>(entities);

            var paginatedResult = new PaginatedResult<GetInternshipGroupsResponse>(resultItems, totalCount, request.PageNumber, request.PageSize);

            await _cacheService.SetAsync(cacheKey, paginatedResult, InternshipGroupCacheKeys.Expiration.GroupList, cancellationToken);

            _logger.LogInformation("Retrieved {Count} internship groups (cached key: {CacheKey})", resultItems.Count, cacheKey);

            return Result<PaginatedResult<GetInternshipGroupsResponse>>.Success(paginatedResult);
        }
    }
}
