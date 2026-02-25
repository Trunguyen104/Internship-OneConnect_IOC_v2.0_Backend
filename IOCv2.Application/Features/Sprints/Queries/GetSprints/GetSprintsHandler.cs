using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Extensions.Pagination;
using IOCv2.Application.Features.Sprints.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.Sprints.Queries.GetSprints;

public class GetSprintsHandler : IRequestHandler<GetSprintsQuery, Result<PagedResult<GetSprintsResponse>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICacheService _cacheService;

    public GetSprintsHandler(IUnitOfWork unitOfWork, IMapper mapper, ICacheService cacheService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _cacheService = cacheService;
    }

    public async Task<Result<PagedResult<GetSprintsResponse>>> Handle(
        GetSprintsQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = SprintCacheKeys.SprintList(
            request.ProjectId,
            request.Pagination.PageIndex,
            request.Pagination.PageSize,
            request.StatusFilter,
            request.Pagination.Search,
            request.Pagination.OrderBy);

        var cachedResult = await _cacheService.GetAsync<PagedResult<GetSprintsResponse>>(cacheKey, cancellationToken);
        if (cachedResult is not null)
            return Result<PagedResult<GetSprintsResponse>>.Success(cachedResult);

        // Build IQueryable — filter at DB level
        var query = _unitOfWork.Repository<Sprint>().Query()
            .AsNoTracking()
            .Where(s => s.ProjectId == request.ProjectId);

        if (!string.IsNullOrEmpty(request.StatusFilter) &&
            Enum.TryParse<SprintStatus>(request.StatusFilter, ignoreCase: true, out var statusFilter))
        {
            query = query.Where(s => s.Status == statusFilter);
        }

        if (!string.IsNullOrWhiteSpace(request.Pagination.Search))
        {
            var search = request.Pagination.Search.ToLower();
            query = query.Where(s =>
                (s.Name != null && s.Name.ToLower().Contains(search)) ||
                (s.Goal != null && s.Goal.ToLower().Contains(search)));
        }

        query = string.IsNullOrWhiteSpace(request.Pagination.OrderBy) ||
                request.Pagination.OrderBy.ToLower().StartsWith("startdate")
            ? query.OrderBy(s => s.StartDate)
            : request.Pagination.OrderBy.ToLower().StartsWith("enddate")
                ? query.OrderBy(s => s.EndDate)
                : request.Pagination.OrderBy.ToLower().StartsWith("name")
                    ? query.OrderBy(s => s.Name)
                    : query.OrderBy(s => s.StartDate);

        var totalCount = await query.CountAsync(cancellationToken);
        var entities = await query
            .Skip((request.Pagination.PageIndex - 1) * request.Pagination.PageSize)
            .Take(request.Pagination.PageSize)
            .ToListAsync(cancellationToken);

        var items = _mapper.Map<List<GetSprintsResponse>>(entities);
        var result = new PagedResult<GetSprintsResponse>(items, request.Pagination, totalCount);

        await _cacheService.SetAsync(cacheKey, result, SprintCacheKeys.Expiration.SprintList, cancellationToken);

        return Result<PagedResult<GetSprintsResponse>>.Success(result);
    }
}
