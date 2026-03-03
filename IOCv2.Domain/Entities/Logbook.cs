using IOCv2.Domain.Enums;

namespace IOCv2.Domain.Entities;

public class Logbook : BaseEntity
{
    public Guid LogbookId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? StudentId { get; set; }
    public DateTime DateReport { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string? Issue { get; set; }
    public string Plan { get; set; } = string.Empty;
    public LogbookStatus Status { get; set; }

    public virtual Project Project { get; set; } = null!;
    public virtual Student? Student { get; set; }
    public virtual ICollection<WorkItem> WorkItem { get; set; } = new List<WorkItem>();
}
