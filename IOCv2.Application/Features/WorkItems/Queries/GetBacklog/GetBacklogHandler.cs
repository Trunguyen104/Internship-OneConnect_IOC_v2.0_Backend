using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.WorkItems.Queries.GetBacklog;

public class GetBacklogHandler : IRequestHandler<GetBacklogQuery, Result<GetBacklogResponse>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetBacklogHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<GetBacklogResponse>> Handle(
        GetBacklogQuery request, CancellationToken cancellationToken)
    {
        // Parse optional enum filters
        WorkItemType? typeFilter = null;
        if (!string.IsNullOrWhiteSpace(request.Type) &&
            Enum.TryParse<WorkItemType>(request.Type, ignoreCase: true, out var parsedType))
            typeFilter = parsedType;

        Priority? priorityFilter = null;
        if (!string.IsNullOrWhiteSpace(request.Priority) &&
            Enum.TryParse<Priority>(request.Priority, ignoreCase: true, out var parsedPriority))
            priorityFilter = parsedPriority;

        WorkItemStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<WorkItemStatus>(request.Status, ignoreCase: true, out var parsedStatus))
            statusFilter = parsedStatus;

        // Load all Sprints (excluding Completed)
        var sprints = await _unitOfWork.Repository<Sprint>()
            .Query()
            .AsNoTracking()
            .Where(s => s.ProjectId == request.ProjectId && s.Status != SprintStatus.Completed)
            .OrderBy(s => s.Status == SprintStatus.Active ? 0 : 1)
            .ThenBy(s => s.CreatedAt)
            .Include(s => s.SprintWorkItems)
                .ThenInclude(swi => swi.WorkItem)
                    .ThenInclude(w => w.Assignee)
            .ToListAsync(cancellationToken);

        // All WorkItem IDs assigned to any sprint
        var assignedIds = sprints
            .SelectMany(s => s.SprintWorkItems.Select(swi => swi.WorkItemId))
            .ToHashSet();

        // Build base WorkItem query for product backlog (not assigned to any sprint)
        var backlogQuery = _unitOfWork.Repository<WorkItem>()
            .Query()
            .AsNoTracking()
            .Where(w => w.ProjectId == request.ProjectId && !assignedIds.Contains(w.WorkItemId) && w.Type != WorkItemType.Epic);

        // Apply filters
        if (request.EpicId.HasValue)
        {
            backlogQuery = backlogQuery.Where(w =>
                w.WorkItemId == request.EpicId.Value ||
                w.ParentId == request.EpicId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            backlogQuery = backlogQuery.Where(w => w.Title.ToLower().Contains(term));
        }

        if (typeFilter.HasValue)
            backlogQuery = backlogQuery.Where(w => w.Type == typeFilter.Value);

        if (priorityFilter.HasValue)
            backlogQuery = backlogQuery.Where(w => w.Priority == priorityFilter.Value);

        if (statusFilter.HasValue)
            backlogQuery = backlogQuery.Where(w => w.Status == statusFilter.Value);

        if (request.AssigneeId.HasValue)
            backlogQuery = backlogQuery.Where(w => w.AssigneeId == request.AssigneeId.Value);

        var backlogItems = await backlogQuery
            .Include(w => w.Assignee)
            .OrderBy(w => w.BacklogOrder)
            .ToListAsync(cancellationToken);

        // Build Sprint Backlog DTOs
        var sprintDtos = sprints.Select(sprint =>
        {
            var items = sprint.SprintWorkItems
                .Select(swi => swi.WorkItem)
                .Where(w => MatchFilters(w, request, typeFilter, priorityFilter, statusFilter))
                .OrderBy(w => sprint.SprintWorkItems.First(swi => swi.WorkItemId == w.WorkItemId).BoardOrder)
                .Select(w => ToBacklogWorkItemDto(w,
                    sprint.SprintWorkItems.First(swi => swi.WorkItemId == w.WorkItemId).BoardOrder))
                .ToList();

            return new SprintBacklogDto
            {
                SprintId = sprint.SprintId,
                Name = sprint.Name,
                Goal = sprint.Goal,
                Status = sprint.Status.ToString(),
                StartDate = sprint.StartDate,
                EndDate = sprint.EndDate,
                ItemCount = items.Count,
                StoryPointsTotal = items.Sum(i => i.StoryPoint ?? 0),
                Items = items
            };
        }).ToList();

        // Build Product Backlog DTO
        var backlogDtos = backlogItems.Select(w => ToBacklogWorkItemDto(w, w.BacklogOrder)).ToList();

        var response = new GetBacklogResponse
        {
            Sprints = sprintDtos,
            ProductBacklog = new ProductBacklogDto
            {
                ItemCount = backlogDtos.Count,
                StoryPointsTotal = backlogDtos.Sum(i => i.StoryPoint ?? 0),
                Items = backlogDtos
            }
        };

        return Result<GetBacklogResponse>.Success(response);
    }

    private static bool MatchFilters(
        WorkItem w, GetBacklogQuery req,
        WorkItemType? type, Priority? priority, WorkItemStatus? status)
    {
        if (w.Type == WorkItemType.Epic) return false;

        if (req.EpicId.HasValue && w.WorkItemId != req.EpicId.Value && w.ParentId != req.EpicId.Value)
            return false;

        if (!string.IsNullOrWhiteSpace(req.SearchTerm) &&
            !w.Title.Contains(req.SearchTerm.Trim(), StringComparison.OrdinalIgnoreCase))
            return false;

        if (type.HasValue && w.Type != type.Value) return false;
        if (priority.HasValue && w.Priority != priority.Value) return false;
        if (status.HasValue && w.Status != status.Value) return false;
        if (req.AssigneeId.HasValue && w.AssigneeId != req.AssigneeId.Value) return false;

        return true;
    }

    private static BacklogWorkItemDto ToBacklogWorkItemDto(WorkItem w, float order) => new()
    {
        WorkItemId = w.WorkItemId,
        ParentId = w.ParentId,
        Title = w.Title,
        Type = w.Type.ToString(),
        Status = w.Status?.ToString(),
        Priority = w.Priority?.ToString(),
        StoryPoint = w.StoryPoint,
        AssigneeId = w.AssigneeId,
        AssigneeName = w.Assignee != null ? $"{w.Assignee.User?.FullName}" : null,
        DueDate = w.DueDate,
        Order = order,
        CreatedAt = w.CreatedAt
    };
}
