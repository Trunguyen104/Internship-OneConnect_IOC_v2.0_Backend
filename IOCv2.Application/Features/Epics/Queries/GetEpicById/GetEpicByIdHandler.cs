using System.Diagnostics;
using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Epics.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Application.Resources;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Epics.Queries.GetEpicById;

public class GetEpicByIdHandler : IRequestHandler<GetEpicByIdQuery, Result<GetEpicByIdResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IStringLocalizer<ErrorMessages> _localizer;
    private readonly ICacheService _cacheService;
    private readonly ILogger<GetEpicByIdHandler> _logger;
    
    public GetEpicByIdHandler(
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        IStringLocalizer<ErrorMessages> localizer,
        ICacheService cacheService,
        ILogger<GetEpicByIdHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _localizer = localizer;
        _cacheService = cacheService;
        _logger = logger;
    }
    
    public async Task<Result<GetEpicByIdResponse>> Handle(GetEpicByIdQuery request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var cacheKey = EpicCacheKeys.Epic(request.EpicId);
        
        try
        {
            // Try get from cache
            var cachedEpic = await _cacheService.GetAsync<GetEpicByIdResponse>(cacheKey, cancellationToken);
            
            if (cachedEpic != null)
            {
                stopwatch.Stop();
                _logger.LogDebug(
                    "Epic cache hit for {EpicId} (Duration: {Duration}ms)",
                    request.EpicId, stopwatch.ElapsedMilliseconds);
                
                return Result<GetEpicByIdResponse>.Success(cachedEpic);
            }
            
            _logger.LogDebug("Epic cache miss for {EpicId}", request.EpicId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache get failed for Epic {EpicId}, falling back to DB", request.EpicId);
        }
        
        // Query from database
        var epic = await _unitOfWork.Repository<WorkItem>()
            .FindAsync(w => w.WorkItemId == request.EpicId && w.Type == WorkItemType.Epic, cancellationToken);
        
        var epicEntity = epic.FirstOrDefault();
        
        if (epicEntity == null)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                "Epic not found: {EpicId} (Duration: {Duration}ms)",
                request.EpicId, stopwatch.ElapsedMilliseconds);
            
            return Result<GetEpicByIdResponse>.NotFound(_localizer["Epic.NotFound"]);
        }
        
        var response = _mapper.Map<GetEpicByIdResponse>(epicEntity);
        
        // Cache the result
        try
        {
            await _cacheService.SetAsync(
                cacheKey, 
                response, 
                EpicCacheKeys.Expiration.Epic, 
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cache Epic {EpicId}", request.EpicId);
        }
        
        stopwatch.Stop();
        _logger.LogInformation(
            "Epic retrieved from DB: {EpicId} in Project {ProjectId} (Duration: {Duration}ms)",
            response.Id, response.ProjectId, stopwatch.ElapsedMilliseconds);
        
        return Result<GetEpicByIdResponse>.Success(response);
    }
}
