using IOCv2.Domain.Enums;

namespace IOCv2.Domain.Entities;

public class InternshipApplication : BaseEntity
{
    public Guid ApplicationId { get; set; }
    public Guid EnterpriseId { get; set; }
    public Guid TermId { get; set; }
    public Guid StudentId { get; set; }
    public InternshipApplicationStatus Status { get; set; }
    public string? RejectReason { get; set; }
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedBy { get; set; }

    public virtual Enterprise Enterprise { get; set; } = null!;
    public virtual Term Term { get; set; } = null!;
    public virtual Student Student { get; set; } = null!;
    public virtual EnterpriseUser? Reviewer { get; set; }
}
