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

namespace IOCv2.Application.Features.Sprints.Commands.DeleteSprint;

public class DeleteSprintHandler : IRequestHandler<DeleteSprintCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly IStringLocalizer<ErrorMessages> _errorLocalizer;
    private readonly ILogger<DeleteSprintHandler> _logger;
    
    public DeleteSprintHandler(
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        IStringLocalizer<ErrorMessages> errorLocalizer,
        ILogger<DeleteSprintHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _errorLocalizer = errorLocalizer;
        _logger = logger;
    }
    
    public async Task<Result<bool>> Handle(DeleteSprintCommand request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Find sprint
        var sprints = await _unitOfWork.Repository<Sprint>()
            .FindAsync(s => s.SprintId == request.SprintId, cancellationToken);
        var sprint = sprints.FirstOrDefault();
        
        if (sprint == null)
        {
            stopwatch.Stop();
            _logger.LogWarning("Sprint not found for delete: {SprintId}", request.SprintId);
            return Result<bool>.NotFound(_errorLocalizer["Sprint.NotFound"]);
        }
        
        // Business rule: Cannot delete Active or Completed sprints
        if (sprint.Status != SprintStatus.Planned)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                "Cannot delete Sprint {SprintId} with status {Status}",
                request.SprintId, sprint.Status);
            return Result<bool>.Failure(
                _errorLocalizer["Sprint.CannotDeleteActiveSprint"], 
                ResultErrorType.BadRequest);
        }
        
        // Check if sprint has work items
        var sprintWorkItems = await _unitOfWork.Repository<SprintWorkItem>()
            .FindAsync(swi => swi.SprintId == request.SprintId, cancellationToken);
        
        if (sprintWorkItems.Any())
        {
            stopwatch.Stop();
            _logger.LogWarning(
                "Cannot delete Sprint {SprintId}: has {Count} work items",
                request.SprintId, sprintWorkItems.Count());
            return Result<bool>.Failure(
                _errorLocalizer["Sprint.CannotDeleteWithWorkItems"], 
                ResultErrorType.BadRequest);
        }
        
        // Delete sprint (soft delete handled by EF Core if configured)
        await _unitOfWork.Repository<Sprint>().DeleteAsync(sprint, cancellationToken);
        await _unitOfWork.SaveChangeAsync(cancellationToken);
        
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
            _logger.LogError(ex, "Failed to invalidate cache after Sprint deletion");
        }
        
        stopwatch.Stop();
        _logger.LogInformation(
            "Sprint deleted: {SprintId} in Project {ProjectId} (Duration: {Duration}ms)",
            request.SprintId, sprint.ProjectId, stopwatch.ElapsedMilliseconds);
        
        return Result<bool>.Success(true);
    }
}
