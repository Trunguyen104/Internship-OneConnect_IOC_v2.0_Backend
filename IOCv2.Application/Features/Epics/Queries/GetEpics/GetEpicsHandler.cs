using AutoMapper;
using AutoMapper.QueryableExtensions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Extensions.Pagination;
using IOCv2.Application.Extensions.Query;
using IOCv2.Application.Features.Epics.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.Epics.Queries.GetEpics;

public class GetEpicsHandler : IRequestHandler<GetEpicsQuery, Result<PaginatedResult<GetEpicsResponse>>>
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

    public async Task<Result<PaginatedResult<GetEpicsResponse>>> Handle(
        GetEpicsQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = EpicCacheKeys.EpicList(
            request.ProjectId,
            request.Pagination.PageIndex,
            request.Pagination.PageSize,
            request.Pagination.Search,
            request.Pagination.OrderBy);

        var cachedResult = await _cacheService.GetAsync<PaginatedResult<GetEpicsResponse>>(cacheKey, cancellationToken);
        if (cachedResult is not null)
            return Result<PaginatedResult<GetEpicsResponse>>.Success(cachedResult);

        // Build IQueryable — filter at DB level
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

        var result = await query
            .ProjectTo<GetEpicsResponse>(_mapper.ConfigurationProvider)
            .ToPaginatedResultAsync(request.Pagination.PageIndex, request.Pagination.PageSize, cancellationToken);

        await _cacheService.SetAsync(cacheKey, result, EpicCacheKeys.Expiration.EpicList, cancellationToken);

        return Result<PaginatedResult<GetEpicsResponse>>.Success(result);
    }
}
