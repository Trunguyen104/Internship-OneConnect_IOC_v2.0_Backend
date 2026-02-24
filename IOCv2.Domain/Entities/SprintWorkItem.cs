namespace IOCv2.Domain.Entities;

public class SprintWorkItem
{
    public Guid SprintId { get; set; }
    public Guid WorkItemId { get; set; }
    public float BoardOrder { get; set; }

    // Navigation properties
    public virtual Sprint Sprint { get; set; } = null!;
    public virtual WorkItem WorkItem { get; set; } = null!;
}
