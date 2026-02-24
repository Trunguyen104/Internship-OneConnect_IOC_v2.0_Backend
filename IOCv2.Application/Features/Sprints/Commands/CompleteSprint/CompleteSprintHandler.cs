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

namespace IOCv2.Application.Features.Sprints.Commands.CompleteSprint;

public class CompleteSprintHandler : IRequestHandler<CompleteSprintCommand, Result<CompleteSprintResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly IStringLocalizer<ErrorMessages> _errorLocalizer;
    private readonly ILogger<CompleteSprintHandler> _logger;
    
    public CompleteSprintHandler(
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        IStringLocalizer<ErrorMessages> errorLocalizer,
        ILogger<CompleteSprintHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _errorLocalizer = errorLocalizer;
        _logger = logger;
    }
    
    public async Task<Result<CompleteSprintResponse>> Handle(CompleteSprintCommand request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Find sprint
        var sprints = await _unitOfWork.Repository<Sprint>()
            .FindAsync(s => s.SprintId == request.SprintId, cancellationToken);
        var sprint = sprints.FirstOrDefault();
        
        if (sprint == null)
        {
            _logger.LogWarning("Sprint not found for complete: {SprintId}", request.SprintId);
            return Result<CompleteSprintResponse>.NotFound(_errorLocalizer["Sprint.NotFound"]);
        }
        
        // Business rule: Only Active sprints can be completed
        if (sprint.Status != SprintStatus.Active)
        {
            _logger.LogWarning(
                "Cannot complete Sprint {SprintId} with status {Status}",
                request.SprintId, sprint.Status);
            return Result<CompleteSprintResponse>.Failure(_errorLocalizer["Sprint.NotActive"], ResultErrorType.BadRequest);
        }
        
        // Get all work items in sprint
        var sprintWorkItems = await _unitOfWork.Repository<SprintWorkItem>()
            .FindAsync(swi => swi.SprintId == request.SprintId, cancellationToken);
        
        // Load WorkItem details
        var workItemIds = sprintWorkItems.Select(swi => swi.WorkItemId).ToList();
        var allWorkItems = new List<WorkItem>();
        
        foreach (var id in workItemIds)
        {
            var wi = await _unitOfWork.Repository<WorkItem>()
                .FindAsync(w => w.WorkItemId == id, cancellationToken);
            if (wi.Any())
                allWorkItems.Add(wi.First());
        }
        
        // Separate completed vs incomplete
        var completedItems = allWorkItems
            .Where(wi => wi.Status == WorkItemStatus.Done)
            .ToList();
        
        var incompleteItems = allWorkItems
            .Where(wi => wi.Status != WorkItemStatus.Done)
            .ToList();
        
        int movedCount = 0;
        
        // Handle incomplete items based on option
        if (incompleteItems.Any())
        {
            var incompleteSprintWorkItems = sprintWorkItems
                .Where(swi => incompleteItems.Any(wi => wi.WorkItemId == swi.WorkItemId))
                .ToList();
            
            switch (request.IncompleteItemsOption)
            {
                case MoveIncompleteItemsOption.ToBacklog:
                    // Remove from sprint (moves to Product Backlog)
                    foreach (var item in incompleteSprintWorkItems)
                    {
                        await _unitOfWork.Repository<SprintWorkItem>().DeleteAsync(item, cancellationToken);
                    }
                    movedCount = incompleteItems.Count;
                    _logger.LogInformation(
                        "Moved {Count} incomplete items to backlog from Sprint {SprintId}",
                        movedCount, request.SprintId);
                    break;
                
                case MoveIncompleteItemsOption.ToNextPlannedSprint:
                    // Dùng targetSprintId nếu được chỉ định, ngược lại tự tìm sprint kế tiếp
                    Sprint? nextSprint = null;
                    
                    if (request.TargetSprintId.HasValue)
                    {
                        var targetSprints = await _unitOfWork.Repository<Sprint>()
                            .FindAsync(s => s.SprintId == request.TargetSprintId.Value, cancellationToken);
                        nextSprint = targetSprints.FirstOrDefault();
                        
                        if (nextSprint == null)
                        {
                            return Result<CompleteSprintResponse>.NotFound(
                                _errorLocalizer["Sprint.TargetSprintNotFound"]);
                        }
                    }
                    else
                    {
                        var allProjectSprints = await _unitOfWork.Repository<Sprint>()
                            .FindAsync(s => s.ProjectId == sprint.ProjectId && s.Status == SprintStatus.Planned, cancellationToken);
                        nextSprint = allProjectSprints
                            .OrderBy(s => s.StartDate)
                            .FirstOrDefault();
                    }
                    
                    if (nextSprint != null)
                    {
                        foreach (var item in incompleteSprintWorkItems)
                        {
                            item.SprintId = nextSprint.SprintId;
                            await _unitOfWork.Repository<SprintWorkItem>().UpdateAsync(item, cancellationToken);
                        }
                        movedCount = incompleteItems.Count;
                        _logger.LogInformation(
                            "Moved {Count} incomplete items to Sprint {NextSprintId}",
                            movedCount, nextSprint.SprintId);
                    }
                    else
                    {
                        // Không có sprint nào, chuyển về backlog
                        foreach (var item in incompleteSprintWorkItems)
                        {
                            await _unitOfWork.Repository<SprintWorkItem>().DeleteAsync(item, cancellationToken);
                        }
                        movedCount = incompleteItems.Count;
                        _logger.LogWarning(
                            "No target sprint found. Moved {Count} items to backlog",
                            movedCount);
                    }
                    break;
                
                case MoveIncompleteItemsOption.CreateNewSprint:
                    // Dùng NewSprintName nếu được chỉ định, ngược lại tự tạo tên
                    var sprintName = !string.IsNullOrWhiteSpace(request.NewSprintName)
                        ? request.NewSprintName
                        : $"{sprint.Name} (Continued)";
                    
                    var newSprint = new Sprint
                    {
                        SprintId = Guid.NewGuid(),
                        ProjectId = sprint.ProjectId,
                        Name = sprintName,
                        Goal = "Incomplete items from previous sprint",
                        StartDate = sprint.EndDate.HasValue ? sprint.EndDate.Value.AddDays(1) : DateOnly.FromDateTime(DateTime.UtcNow),
                        EndDate = sprint.EndDate.HasValue ? sprint.EndDate.Value.AddDays(15) : DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)),
                        Status = SprintStatus.Planned,
                        CreatedAt = DateTime.UtcNow
                    };
                    
                    await _unitOfWork.Repository<Sprint>().AddAsync(newSprint, cancellationToken);
                    
                    // Move items to new sprint
                    foreach (var item in incompleteSprintWorkItems)
                    {
                        item.SprintId = newSprint.SprintId;
                        await _unitOfWork.Repository<SprintWorkItem>().UpdateAsync(item, cancellationToken);
                    }
                    movedCount = incompleteItems.Count;
                    _logger.LogInformation(
                        "Created new Sprint '{SprintName}' ({NewSprintId}) and moved {Count} incomplete items",
                        sprintName, newSprint.SprintId, movedCount);
                    break;
            }
        }
        
        // Update sprint status to Completed
        sprint.Status = SprintStatus.Completed;
        sprint.UpdatedAt = DateTime.UtcNow;
        
        await _unitOfWork.Repository<Sprint>().UpdateAsync(sprint, cancellationToken);
        await _unitOfWork.SaveChangeAsync(cancellationToken);
        
        // Invalidate cache
        try
        {
            var sprintCacheKey = SprintCacheKeys.Sprint(request.SprintId);
            await _cacheService.RemoveAsync(sprintCacheKey, cancellationToken);
            
            var listCachePattern = SprintCacheKeys.SprintListPattern(sprint.ProjectId);
            await _cacheService.RemoveByPatternAsync(listCachePattern, cancellationToken);
            
            _logger.LogDebug("Invalidated Sprint cache after completion");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to invalidate cache after Sprint completion");
        }
        
        stopwatch.Stop();
        _logger.LogInformation(
            "Sprint completed: {SprintId}, Completed: {CompletedCount}, Moved: {MovedCount} (Duration: {Duration}ms)",
            request.SprintId, completedItems.Count, movedCount, stopwatch.ElapsedMilliseconds);
        
        var response = new CompleteSprintResponse
        {
            SprintId = request.SprintId,
            CompletedItemsCount = completedItems.Count,
            IncompleteItemsCount = incompleteItems.Count,
            MovedItemsCount = movedCount,
            Message = $"Sprint completed. {completedItems.Count} items completed, {movedCount} items moved."
        };
        
        return Result<CompleteSprintResponse>.Success(response);
    }
}
