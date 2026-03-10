using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.WorkItems.Queries.GetBacklog;

public class GetBacklogResponse
{
    public List<SprintBacklogDto> Sprints { get; set; } = new();
    public ProductBacklogDto ProductBacklog { get; set; } = new();
}

public class SprintBacklogDto
{
    public Guid SprintId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Goal { get; set; }
    public SprintStatus Status { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public int ItemCount { get; set; }
    public int StoryPointsTotal { get; set; }
    public List<BacklogWorkItemDto> Items { get; set; } = new();
}

public class ProductBacklogDto
{
    public int ItemCount { get; set; }
    public int StoryPointsTotal { get; set; }
    public List<BacklogWorkItemDto> Items { get; set; } = new();
}

public class BacklogWorkItemDto
{
    public Guid WorkItemId { get; set; }
    public Guid? ParentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public WorkItemType Type { get; set; }
    public WorkItemStatus? Status { get; set; }
    public Priority? Priority { get; set; }
    public int? StoryPoint { get; set; }
    public Guid? AssigneeId { get; set; }
    public string? AssigneeName { get; set; }
    public DateOnly? DueDate { get; set; }
    public float Order { get; set; }
    public DateTime CreatedAt { get; set; }
}
