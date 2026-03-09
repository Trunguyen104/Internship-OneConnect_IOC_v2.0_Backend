using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using IOCv2.Application.Constants;

namespace IOCv2.Application.Features.WorkItems.Queries.GetBacklog;

public class GetBacklogHandler : IRequestHandler<GetBacklogQuery, Result<GetBacklogResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessageService _messageService;
    private readonly ILogger<GetBacklogHandler> _logger;

    public GetBacklogHandler(
        IUnitOfWork unitOfWork, 
        IMessageService messageService,
        ILogger<GetBacklogHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _messageService = messageService;
        _logger = logger;
    }

    public async Task<Result<GetBacklogResponse>> Handle(
        GetBacklogQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting backlog for project {ProjectId}", request.ProjectId);

        try
        {
        // BacklogOnly=true: bỏ qua Sprints, chỉ lấy Product Backlog
        List<Sprint> sprints;
        HashSet<Guid> assignedIds;

        if (request.BacklogOnly)
        {
            // Chỉ cần biết IDs đã trong sprint để loại ra khỏi product backlog
            var rawAssignedIds = await _unitOfWork.Repository<SprintWorkItem>()
                .Query()
                .AsNoTracking()
                .Where(swi => swi.Sprint.ProjectId == request.ProjectId
                           && swi.Sprint.Status != SprintStatus.Completed)
                .Select(swi => swi.WorkItemId)
                .ToListAsync(cancellationToken);

            sprints = new List<Sprint>();
            assignedIds = rawAssignedIds.ToHashSet();
        }
        else
        {
            // Load all Sprints (excluding Completed)
            sprints = await _unitOfWork.Repository<Sprint>()
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
            assignedIds = sprints
                .SelectMany(s => s.SprintWorkItems.Select(swi => swi.WorkItemId))
                .ToHashSet();
        }

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

        if (request.Type.HasValue)
            backlogQuery = backlogQuery.Where(w => w.Type == request.Type.Value);

        if (request.Priority.HasValue)
            backlogQuery = backlogQuery.Where(w => w.Priority == request.Priority.Value);

        if (request.Status.HasValue)
            backlogQuery = backlogQuery.Where(w => w.Status == request.Status.Value);

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
                .Where(w => MatchFilters(w, request))
                .OrderBy(w => sprint.SprintWorkItems.First(swi => swi.WorkItemId == w.WorkItemId).BoardOrder)
                .Select(w => ToBacklogWorkItemDto(w,
                    sprint.SprintWorkItems.First(swi => swi.WorkItemId == w.WorkItemId).BoardOrder))
                .ToList();

            return new SprintBacklogDto
            {
                SprintId = sprint.SprintId,
                Name = sprint.Name,
                Goal = sprint.Goal,
                Status = sprint.Status,
                StartDate = sprint.StartDate,
                EndDate = sprint.EndDate,
                ItemCount = items.Count,
                StoryPointsTotal = items.Sum(i => i.StoryPoint ?? 0),
                Items = items
            };
        }).Where(s => !request.EpicId.HasValue || s.Items.Count > 0).ToList();

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting backlog for project {ProjectId}", request.ProjectId);
            return Result<GetBacklogResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.Conflict);
        }
    }

    private static bool MatchFilters(WorkItem w, GetBacklogQuery req)
    {
        if (w.Type == WorkItemType.Epic) return false;

        if (req.EpicId.HasValue && w.WorkItemId != req.EpicId.Value && w.ParentId != req.EpicId.Value)
            return false;

        if (!string.IsNullOrWhiteSpace(req.SearchTerm) &&
            !w.Title.Contains(req.SearchTerm.Trim(), StringComparison.OrdinalIgnoreCase))
            return false;

        if (req.Type.HasValue && w.Type != req.Type.Value) return false;
        if (req.Priority.HasValue && w.Priority != req.Priority.Value) return false;
        if (req.Status.HasValue && w.Status != req.Status.Value) return false;
        if (req.AssigneeId.HasValue && w.AssigneeId != req.AssigneeId.Value) return false;

        return true;
    }

    private static BacklogWorkItemDto ToBacklogWorkItemDto(WorkItem w, float order) => new()
    {
        WorkItemId = w.WorkItemId,
        ParentId = w.ParentId,
        Title = w.Title,
        Type = w.Type,
        Status = w.Status,
        Priority = w.Priority,
        StoryPoint = w.StoryPoint,
        AssigneeId = w.AssigneeId,
        AssigneeName = w.Assignee != null ? $"{w.Assignee.User?.FullName}" : null,
        DueDate = w.DueDate,
        Order = order,
        CreatedAt = w.CreatedAt
    };
}
