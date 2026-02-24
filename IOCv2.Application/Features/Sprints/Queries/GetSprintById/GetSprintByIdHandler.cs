using System.Diagnostics;
using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Sprints.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Application.Resources;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Sprints.Queries.GetSprintById;

public class GetSprintByIdHandler : IRequestHandler<GetSprintByIdQuery, Result<GetSprintByIdResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICacheService _cacheService;
    private readonly IStringLocalizer<ErrorMessages> _localizer;
    private readonly ILogger<GetSprintByIdHandler> _logger;
    
    public GetSprintByIdHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICacheService cacheService,
        IStringLocalizer<ErrorMessages> localizer,
        ILogger<GetSprintByIdHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _cacheService = cacheService;
        _localizer = localizer;
        _logger = logger;
    }
    
    public async Task<Result<GetSprintByIdResponse>> Handle(GetSprintByIdQuery request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var cacheKey = SprintCacheKeys.Sprint(request.SprintId);
        
        // Try cache first
        try
        {
            var cachedResult = await _cacheService.GetAsync<GetSprintByIdResponse>(cacheKey, cancellationToken);
            if (cachedResult != null)
            {
                stopwatch.Stop();
                _logger.LogDebug(
                    "Sprint cache hit for {SprintId} (Duration: {Duration}ms)",
                    request.SprintId, stopwatch.ElapsedMilliseconds);
                return Result<GetSprintByIdResponse>.Success(cachedResult);
            }
            
            _logger.LogDebug("Sprint cache miss for {SprintId}", request.SprintId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache get failed for Sprint {SprintId}, falling back to DB", request.SprintId);
        }
        
        // Query from database
        var sprints = await _unitOfWork.Repository<Sprint>()
            .FindAsync(s => s.SprintId == request.SprintId, cancellationToken);
        var sprint = sprints.FirstOrDefault();
        
        if (sprint == null)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                "Sprint not found: {SprintId} (Duration: {Duration}ms)",
                request.SprintId, stopwatch.ElapsedMilliseconds);
            return Result<GetSprintByIdResponse>.NotFound(_localizer["Sprint.NotFound"]);
        }
        
        // Get work items statistics
        var workItems = await _unitOfWork.Repository<SprintWorkItem>()
            .FindAsync(swi => swi.SprintId == request.SprintId, cancellationToken);
        
        // Load WorkItem details for each SprintWorkItem
        var workItemIds = workItems.Select(swi => swi.WorkItemId).ToList();
        var allWorkItems = new List<WorkItem>();
        
        foreach (var id in workItemIds)
        {
            var wi = await _unitOfWork.Repository<WorkItem>()
                .FindAsync(w => w.WorkItemId == id, cancellationToken);
            if (wi.Any())
                allWorkItems.Add(wi.First());
        }
        
        var response = _mapper.Map<GetSprintByIdResponse>(sprint);
        response.TotalWorkItems = allWorkItems.Count;
        response.CompletedWorkItems = allWorkItems.Count(wi => wi.Status == WorkItemStatus.Done);
        
        // Cache the result
        try
        {
            await _cacheService.SetAsync(
                cacheKey,
                response,
                SprintCacheKeys.Expiration.Sprint,
                cancellationToken);
            
            _logger.LogDebug("Cached Sprint {SprintId}", request.SprintId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cache Sprint {SprintId}", request.SprintId);
        }
        
        stopwatch.Stop();
        _logger.LogInformation(
            "Sprint retrieved from DB: {SprintId} (Duration: {Duration}ms)",
            request.SprintId, stopwatch.ElapsedMilliseconds);
        
        return Result<GetSprintByIdResponse>.Success(response);
    }
}
