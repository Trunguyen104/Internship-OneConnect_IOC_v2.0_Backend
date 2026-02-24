using System.Diagnostics;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Sprints.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Application.Resources;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Sprints.Commands.StartSprint;

public class StartSprintHandler : IRequestHandler<StartSprintCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly IStringLocalizer<ErrorMessages> _errorLocalizer;
    private readonly ILogger<StartSprintHandler> _logger;
    
    public StartSprintHandler(
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        IStringLocalizer<ErrorMessages> errorLocalizer,
        ILogger<StartSprintHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _errorLocalizer = errorLocalizer;
        _logger = logger;
    }
    
    public async Task<Result> Handle(StartSprintCommand request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Find sprint using FindAsync
        var sprints = await _unitOfWork.Repository<Sprint>()
            .FindAsync(s => s.SprintId == request.SprintId, cancellationToken);
        var sprint = sprints.FirstOrDefault();
        
        if (sprint == null)
        {
            stopwatch.Stop();
            _logger.LogWarning("Sprint not found for start: {SprintId}", request.SprintId);
            return Result.NotFound(_errorLocalizer["Sprint.NotFound"]);
        }
        
        // Business rule: Only Planned sprints can be started
        if (sprint.Status != SprintStatus.Planned)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                "Cannot start Sprint {SprintId} with status {Status}",
                request.SprintId, sprint.Status);
            return Result.Failure(_errorLocalizer["Sprint.NotPlanned"], ResultErrorType.BadRequest);
        }
        
        // Business rule: Only 1 active sprint per project
        var activeSprints = await _unitOfWork.Repository<Sprint>()
            .FindAsync(s => s.ProjectId == sprint.ProjectId && s.Status == SprintStatus.Active, cancellationToken);
        
        if (activeSprints.Any())
        {
            stopwatch.Stop();
            _logger.LogWarning(
                "Cannot start Sprint {SprintId}: Project {ProjectId} already has an active sprint",
                request.SprintId, sprint.ProjectId);
            return Result.Failure(_errorLocalizer["Sprint.ActiveSprintExists"], ResultErrorType.BadRequest);
        }
        
        // Get work item count for logging
        var workItems = await _unitOfWork.Repository<SprintWorkItem>()
            .FindAsync(swi => swi.SprintId == request.SprintId, cancellationToken);
        var workItemCount = workItems.Count();
        
        // Update sprint status
        sprint.Status = SprintStatus.Active;
        sprint.UpdatedAt = DateTime.UtcNow;
        
        await _unitOfWork.Repository<Sprint>().UpdateAsync(sprint, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        // Invalidate cache
        try
        {
            var sprintCacheKey = SprintCacheKeys.Sprint(request.SprintId);
            await _cacheService.RemoveAsync(sprintCacheKey, cancellationToken);
            
            var listCachePattern = SprintCacheKeys.SprintListPattern(sprint.ProjectId);
            await _cacheService.RemoveByPatternAsync(listCachePattern, cancellationToken);
            
            _logger.LogDebug(
                "Invalidated Sprint cache for {SprintId} and list for Project {ProjectId}",
                request.SprintId, sprint.ProjectId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to invalidate cache after Sprint start");
        }
        
        stopwatch.Stop();
        _logger.LogInformation(
            "Sprint started: {SprintId} in Project {ProjectId} with {WorkItemCount} items (Duration: {Duration}ms)",
            request.SprintId, sprint.ProjectId, workItemCount, stopwatch.ElapsedMilliseconds);
        
        return Result.Success();
    }
}
