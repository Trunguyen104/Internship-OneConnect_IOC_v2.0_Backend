namespace IOCv2.Domain.Entities;

public class SprintWorkItem : BaseEntity
{
    public Guid SprintWorkItemId { get; set; }
    public Guid SprintId { get; set; }
    public Guid WorkItemId { get; set; }
    
    public int BoardOrder { get; set; }  // For Kanban board ordering
    
    // Navigation properties
    public virtual Sprint Sprint { get; set; } = null!;
    public virtual WorkItem WorkItem { get; set; } = null!;
}
