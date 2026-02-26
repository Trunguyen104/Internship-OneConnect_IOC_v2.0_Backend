using IOCv2.Domain.Enums;

namespace IOCv2.Domain.Entities;

public class Sprint : BaseEntity
{
    public Guid SprintId { get; set; }
    public Guid ProjectId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Goal { get; set; }

    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    public SprintStatus Status { get; set; }

    // Navigation properties
    public virtual ICollection<SprintWorkItem> SprintWorkItems { get; set; } = new List<SprintWorkItem>();
}
