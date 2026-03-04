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

    public int? StoryPoint { get; set; }
    public Priority? Priority { get; set; }

    public Guid? AssigneeId { get; set; }  // FK to students.student_id

    public float BacklogOrder { get; set; }
    public DateOnly? DueDate { get; set; }

    public WorkItemStatus? Status { get; set; }

    // Navigation properties
    public virtual Project Project { get; set; } = null!;
    public virtual WorkItem? Parent { get; set; }
    public virtual ICollection<WorkItem> Children { get; set; } = new List<WorkItem>();
    public virtual Student? Assignee { get; set; }

    // Domain Methods
    public void UpdateInfo(string title, string? description)
    {
        Title = title;
        Description = description;
    }

    public void SoftDelete()
    {
        DeletedAt = DateTime.UtcNow;
    }
}
