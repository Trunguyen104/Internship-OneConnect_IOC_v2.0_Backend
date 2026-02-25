using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Extensions.Pagination;
using IOCv2.Application.Features.Epics.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.Epics.Queries.GetEpics;

public class GetEpicsHandler : IRequestHandler<GetEpicsQuery, Result<PagedResult<GetEpicsResponse>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICacheService _cacheService;

    public GetEpicsHandler(IUnitOfWork unitOfWork, IMapper mapper, ICacheService cacheService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _cacheService = cacheService;
    }

    public async Task<Result<PagedResult<GetEpicsResponse>>> Handle(
        GetEpicsQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = EpicCacheKeys.EpicList(
            request.ProjectId,
            request.Pagination.PageIndex,
            request.Pagination.PageSize,
            request.Pagination.Search,
            request.Pagination.OrderBy);

        var cachedResult = await _cacheService.GetAsync<PagedResult<GetEpicsResponse>>(cacheKey, cancellationToken);
        if (cachedResult is not null)
            return Result<PagedResult<GetEpicsResponse>>.Success(cachedResult);

        // Build IQueryable — filter at DB level (fix: was .Result blocking call)
        var query = _unitOfWork.Repository<WorkItem>().Query()
            .AsNoTracking()
            .Where(w => w.ProjectId == request.ProjectId && w.Type == WorkItemType.Epic);

        if (!string.IsNullOrWhiteSpace(request.Pagination.Search))
        {
            var searchTerm = request.Pagination.Search.ToLower();
            query = query.Where(w =>
                w.Title.ToLower().Contains(searchTerm) ||
                (w.Description != null && w.Description.ToLower().Contains(searchTerm)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        query = string.IsNullOrWhiteSpace(request.Pagination.OrderBy)
            ? query.OrderByDescending(w => w.CreatedAt)
            : request.Pagination.OrderBy.ToLower() switch
            {
                "title" => query.OrderBy(w => w.Title),
                "title_desc" => query.OrderByDescending(w => w.Title),
                "createdat" => query.OrderBy(w => w.CreatedAt),
                "createdat_desc" => query.OrderByDescending(w => w.CreatedAt),
                _ => query.OrderByDescending(w => w.CreatedAt)
            };

        var entities = await query
            .Skip((request.Pagination.PageIndex - 1) * request.Pagination.PageSize)
            .Take(request.Pagination.PageSize)
            .ToListAsync(cancellationToken);

        var mappedItems = _mapper.Map<List<GetEpicsResponse>>(entities);
        var pagedResult = new PagedResult<GetEpicsResponse>(mappedItems, request.Pagination, totalCount);

        await _cacheService.SetAsync(cacheKey, pagedResult, EpicCacheKeys.Expiration.EpicList, cancellationToken);

        return Result<PagedResult<GetEpicsResponse>>.Success(pagedResult);
    }
}
