using System.Diagnostics;
using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Extensions.Pagination;
using IOCv2.Application.Features.Epics.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Epics.Queries.GetEpics;

public class GetEpicsHandler : IRequestHandler<GetEpicsQuery, Result<PagedResult<GetEpicsResponse>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICacheService _cacheService;
    private readonly ILogger<GetEpicsHandler> _logger;
    
    public GetEpicsHandler(
        IUnitOfWork unitOfWork, 
        IMapper mapper,
        ICacheService cacheService,
        ILogger<GetEpicsHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _cacheService = cacheService;
        _logger = logger;
    }
    
    public async Task<Result<PagedResult<GetEpicsResponse>>> Handle(GetEpicsQuery request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var cacheKey = EpicCacheKeys.EpicList(
            request.ProjectId,
            request.Pagination.PageIndex,
            request.Pagination.PageSize,
            request.Pagination.Search,
            request.Pagination.OrderBy);
        
        try
        {
            // Try get from cache
            var cachedResult = await _cacheService.GetAsync<PagedResult<GetEpicsResponse>>(cacheKey, cancellationToken);
            
            if (cachedResult != null)
            {
                stopwatch.Stop();
                _logger.LogDebug(
                    "Epic list cache hit for Project {ProjectId}, Page {Page} (Duration: {Duration}ms)",
                    request.ProjectId, request.Pagination.PageIndex, stopwatch.ElapsedMilliseconds);
                
                return Result<PagedResult<GetEpicsResponse>>.Success(cachedResult);
            }
            
            _logger.LogDebug(
                "Epic list cache miss for Project {ProjectId}, Page {Page}",
                request.ProjectId, request.Pagination.PageIndex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Cache get failed for Epic list in Project {ProjectId}, falling back to DB", 
                request.ProjectId);
        }
        
        // Query Epics for specific project
        var query = _unitOfWork.Repository<WorkItem>()
            .FindAsync(w => w.ProjectId == request.ProjectId && w.Type == WorkItemType.Epic, cancellationToken)
            .Result
            .AsQueryable();
        
        // Apply search if provided
        if (!string.IsNullOrWhiteSpace(request.Pagination.Search))
        {
            var searchTerm = request.Pagination.Search.ToLower();
            query = query.Where(w => 
                w.Title.ToLower().Contains(searchTerm) ||
                (w.Description != null && w.Description.ToLower().Contains(searchTerm))
            );
        }
        
        // Get total count
        var totalCount = query.Count();
        
        // Apply ordering (default: by CreatedAt descending)
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
        
        // Apply pagination
        var items = query
            .Skip((request.Pagination.PageIndex - 1) * request.Pagination.PageSize)
            .Take(request.Pagination.PageSize)
            .ToList();
        
        // Map to response DTO
        var mappedItems = _mapper.Map<List<GetEpicsResponse>>(items);
        
        var pagedResult = new PagedResult<GetEpicsResponse>(
            mappedItems,
            request.Pagination,
            totalCount
        );
        
        // Cache the result
        try
        {
            await _cacheService.SetAsync(
                cacheKey,
                pagedResult,
                EpicCacheKeys.Expiration.EpicList,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Failed to cache Epic list for Project {ProjectId}", 
                request.ProjectId);
        }
        
        stopwatch.Stop();
        _logger.LogInformation(
            "Epic list retrieved from DB for Project {ProjectId}: {Count} items, Page {Page}/{TotalPages} (Duration: {Duration}ms)",
            request.ProjectId, mappedItems.Count, pagedResult.PageIndex, pagedResult.TotalPages, stopwatch.ElapsedMilliseconds);
        
        return Result<PagedResult<GetEpicsResponse>>.Success(pagedResult);
    }
}
