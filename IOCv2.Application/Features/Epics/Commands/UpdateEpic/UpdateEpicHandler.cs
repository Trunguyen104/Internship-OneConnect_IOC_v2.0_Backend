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

namespace IOCv2.Application.Features.Epics.Commands.UpdateEpic;

public class UpdateEpicHandler : IRequestHandler<UpdateEpicCommand, Result<UpdateEpicResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICacheService _cacheService;
    private readonly IStringLocalizer<ErrorMessages> _localizer;
    private readonly ILogger<UpdateEpicHandler> _logger;
    
    public UpdateEpicHandler(
        IUnitOfWork unitOfWork, 
        IMapper mapper,
        ICacheService cacheService,
        IStringLocalizer<ErrorMessages> localizer,
        ILogger<UpdateEpicHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _cacheService = cacheService;
        _localizer = localizer;
        _logger = logger;
    }
    
    public async Task<Result<UpdateEpicResponse>> Handle(UpdateEpicCommand request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Find Epic
        var epic = await _unitOfWork.Repository<WorkItem>()
            .FindAsync(w => w.WorkItemId == request.EpicId && w.Type == WorkItemType.Epic, cancellationToken);
        
        var epicEntity = epic.FirstOrDefault();
        
        if (epicEntity == null)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                "Epic not found for update: {EpicId} (Duration: {Duration}ms)",
                request.EpicId, stopwatch.ElapsedMilliseconds);
            
            return Result<UpdateEpicResponse>.NotFound(_localizer["Epic.NotFound"]);
        }
        
        var projectId = epicEntity.ProjectId;
        
        // Update only Title and Description (Epic-specific fields)
        epicEntity.Title = request.Name;
        epicEntity.Description = request.Description;
        epicEntity.UpdatedAt = DateTime.UtcNow;
        
        await _unitOfWork.Repository<WorkItem>().UpdateAsync(epicEntity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        // Invalidate caches
        try
        {
            // Invalidate single epic cache
            var epicCacheKey = EpicCacheKeys.Epic(request.EpicId);
            await _cacheService.RemoveAsync(epicCacheKey, cancellationToken);
            
            // Invalidate project epic list cache
            var listCachePattern = EpicCacheKeys.EpicListPattern(projectId);
            await _cacheService.RemoveByPatternAsync(listCachePattern, cancellationToken);
            
            _logger.LogDebug(
                "Invalidated Epic cache for {EpicId} and list cache for Project {ProjectId}",
                request.EpicId, projectId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to invalidate cache after Epic update");
        }
        
        stopwatch.Stop();
        _logger.LogInformation(
            "Epic updated: {EpicId} in Project {ProjectId} (Duration: {Duration}ms)",
            request.EpicId, projectId, stopwatch.ElapsedMilliseconds);
        
        var response = _mapper.Map<UpdateEpicResponse>(epicEntity);
        return Result<UpdateEpicResponse>.Success(response);
    }
}
