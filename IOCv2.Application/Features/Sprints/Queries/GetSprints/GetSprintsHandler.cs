using System.Diagnostics;
using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Extensions.Pagination;
using IOCv2.Application.Features.Sprints.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Sprints.Queries.GetSprints;

public class GetSprintsHandler : IRequestHandler<GetSprintsQuery, Result<PagedResult<GetSprintsResponse>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICacheService _cacheService;
    private readonly ILogger<GetSprintsHandler> _logger;
    
    public GetSprintsHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICacheService cacheService,
        ILogger<GetSprintsHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _cacheService = cacheService;
        _logger = logger;
    }
    
    public async Task<Result<PagedResult<GetSprintsResponse>>> Handle(GetSprintsQuery request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var cacheKey = SprintCacheKeys.SprintList(
            request.ProjectId,
            request.Pagination.PageIndex,
            request.Pagination.PageSize,
            request.StatusFilter?.ToString(),
            request.Pagination.Search,
            request.Pagination.OrderBy);
        
        // Try cache first
        try
        {
            var cachedResult = await _cacheService.GetAsync<PagedResult<GetSprintsResponse>>(cacheKey, cancellationToken);
            if (cachedResult != null)
            {
                stopwatch.Stop();
                _logger.LogDebug(
                    "Sprint list cache hit for Project {ProjectId} (Duration: {Duration}ms)",
                    request.ProjectId, stopwatch.ElapsedMilliseconds);
                return Result<PagedResult<GetSprintsResponse>>.Success(cachedResult);
            }
            
            _logger.LogDebug("Sprint list cache miss for Project {ProjectId}", request.ProjectId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache get failed for Sprint list, falling back to DB");
        }
        
        // Query from database using FindAsync
        var allSprints = await _unitOfWork.Repository<Sprint>()
            .FindAsync(s => s.ProjectId == request.ProjectId, cancellationToken);
        
        var query = allSprints.AsQueryable();
        
        // Apply status filter
        if (request.StatusFilter.HasValue)
        {
            query = query.Where(s => s.Status == request.StatusFilter.Value);
        }
        
        // Apply search
        if (!string.IsNullOrWhiteSpace(request.Pagination.Search))
        {
            var search = request.Pagination.Search.ToLower();
            query = query.Where(s => 
                (s.Name != null && s.Name.ToLower().Contains(search)) ||
                (s.Goal != null && s.Goal.ToLower().Contains(search)));
        }
        
        // Apply sorting
        query = string.IsNullOrWhiteSpace(request.Pagination.OrderBy) || request.Pagination.OrderBy.ToLower().StartsWith("startdate")
            ? query.OrderBy(s => s.StartDate)
            : request.Pagination.OrderBy.ToLower().StartsWith("enddate")
                ? query.OrderBy(s => s.EndDate)
                : request.Pagination.OrderBy.ToLower().StartsWith("name")
                    ? query.OrderBy(s => s.Name)
                    : query.OrderBy(s => s.StartDate);
        
        // Manual pagination
        var totalCount = query.Count();
        var items = query
            .Skip((request.Pagination.PageIndex - 1) * request.Pagination.PageSize)
            .Take(request.Pagination.PageSize)
            .Select(s => _mapper.Map<GetSprintsResponse>(s))
            .ToList();
        
        var result = new PagedResult<GetSprintsResponse>(
            items,
            request.Pagination,
            totalCount);
        
        // Cache the result
        try
        {
            await _cacheService.SetAsync(
                cacheKey,
                result,
                SprintCacheKeys.Expiration.SprintList,
                cancellationToken);
            
            _logger.LogDebug("Cached Sprint list for Project {ProjectId}", request.ProjectId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cache Sprint list");
        }
        
        stopwatch.Stop();
        _logger.LogInformation(
            "Sprint list retrieved for Project {ProjectId}: {Count} results (Duration: {Duration}ms)",
            request.ProjectId, result.TotalCount, stopwatch.ElapsedMilliseconds);
        
        return Result<PagedResult<GetSprintsResponse>>.Success(result);
    }
}
