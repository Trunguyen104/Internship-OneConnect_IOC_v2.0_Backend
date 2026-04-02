using IOCv2.Domain.Enums;

namespace IOCv2.Domain.Entities;

public class ApplicationStatusHistory : BaseEntity
{
    public Guid HistoryId { get; set; }
    public Guid ApplicationId { get; set; }

    public InternshipApplicationStatus FromStatus { get; set; }
    public InternshipApplicationStatus ToStatus { get; set; }

    /// <summary>Reject reason, withdraw trigger description, etc.</summary>
    public string? Note { get; set; }

    /// <summary>Display name of who performed the action.</summary>
    public string? ChangedByName { get; set; }

    /// <summary>"HR", "Student-initiated", "System"</summary>
    public string TriggerSource { get; set; } = "HR";

    public virtual InternshipApplication Application { get; set; } = null!;
}
