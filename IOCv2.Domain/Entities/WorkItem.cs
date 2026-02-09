using IOCv2.Domain.Enums;

namespace IOCv2.Domain.Entities;

public class WorkItem : BaseEntity
{
    public Guid WorkItemId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? ParentId { get; set; }
    
    public WorkItemType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    public Priority? Priority { get; set; }
    public WorkItemStatus? Status { get; set; }
    
    public int? StoryPoint { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    
    public float BacklogOrder { get; set; }
    
    public float? OriginalEstimate { get; set; }
    public float? RemainingWork { get; set; }
    
    // Navigation properties
    public virtual WorkItem? Parent { get; set; }
    public virtual ICollection<WorkItem> Children { get; set; } = new List<WorkItem>();
}
