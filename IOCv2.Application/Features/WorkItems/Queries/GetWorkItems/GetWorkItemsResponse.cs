using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.WorkItems.Queries.GetWorkItems;

public class GetWorkItemsResponse
{
    public Guid WorkItemId { get; set; }
    public string Title { get; set; } = string.Empty;
    public WorkItemType Type { get; set; }
    public WorkItemStatus? Status { get; set; }
    public Priority? Priority { get; set; }
    public int? StoryPoint { get; set; }
    
    public Guid? AssigneeId { get; set; }
    public string? AssigneeName { get; set; }
    public string? AssigneeAvatarUrl { get; set; }
    
    public DateOnly? DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
}
