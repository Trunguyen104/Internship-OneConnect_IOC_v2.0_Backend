using IOCv2.Domain.Enums;

namespace IOCv2.Domain.Entities;

public class WorkItem : BaseEntity
{
    public Guid WorkItemId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? ParentId { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public WorkItemType Type { get; set; }
    public WorkItemStatus Status { get; set; }
    public Priority Priority { get; set; }
    public Guid? AssigneeProjectMemberId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public float? EstimatedHours { get; set; }
    public float? ActualHours { get; set; }

    // Navigation properties
    public virtual Project Project { get; set; } = null!;
    public virtual WorkItem? Parent { get; set; }
    public virtual ICollection<WorkItem> Children { get; set; } = new List<WorkItem>();
    public virtual ProjectMember? Assignee { get; set; }
}
