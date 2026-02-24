using System.Diagnostics;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Epics.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Application.Resources;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Epics.Commands.DeleteEpic;

public class DeleteEpicHandler : IRequestHandler<DeleteEpicCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStringLocalizer<ErrorMessages> _localizer;
    private readonly ICacheService _cacheService;
    private readonly ILogger<DeleteEpicHandler> _logger;
    
    public DeleteEpicHandler(
        IUnitOfWork unitOfWork, 
        IStringLocalizer<ErrorMessages> localizer,
        ICacheService cacheService,
        ILogger<DeleteEpicHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _localizer = localizer;
        _cacheService = cacheService;
        _logger = logger;
    }
    
    public async Task<Result<bool>> Handle(DeleteEpicCommand request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Find Epic with children
        var epic = await _unitOfWork.Repository<WorkItem>()
            .FindAsync(w => w.WorkItemId == request.EpicId && w.Type == WorkItemType.Epic, cancellationToken);
        
        var epicEntity = epic.FirstOrDefault();
        
        if (epicEntity == null)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                "Epic not found for deletion: {EpicId} (Duration: {Duration}ms)",
                request.EpicId, stopwatch.ElapsedMilliseconds);
            
            return Result<bool>.NotFound(_localizer["Epic.NotFound"]);
        }
        
        var projectId = epicEntity.ProjectId;
        
        // Check if Epic has children
        var childrenCount = await _unitOfWork.Repository<WorkItem>()
            .CountAsync(w => w.ParentId == request.EpicId, cancellationToken);
        
        if (childrenCount > 0)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                "Cannot delete Epic {EpicId} with {ChildrenCount} children (Duration: {Duration}ms)",
                request.EpicId, childrenCount, stopwatch.ElapsedMilliseconds);
            
            return Result<bool>.Failure(
                _localizer["Epic.CannotDeleteWithChildren"],
                ResultErrorType.BadRequest
            );
        }
        
        // Soft delete - set DeletedAt
        epicEntity.DeletedAt = DateTime.UtcNow;
        
        await _unitOfWork.Repository<WorkItem>().UpdateAsync(epicEntity, cancellationToken);
        await _unitOfWork.SaveChangeAsync(cancellationToken);
        
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
            _logger.LogError(ex, "Failed to invalidate cache after Epic deletion");
        }
        
        stopwatch.Stop();
        _logger.LogInformation(
            "Epic deleted: {EpicId} from Project {ProjectId} (Duration: {Duration}ms)",
            request.EpicId, projectId, stopwatch.ElapsedMilliseconds);
        
        return Result<bool>.Success(true);
    }
}
