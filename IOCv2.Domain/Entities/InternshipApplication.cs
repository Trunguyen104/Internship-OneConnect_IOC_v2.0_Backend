using IOCv2.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace IOCv2.Domain.Entities;

public class InternshipApplication : BaseEntity
{
    [Key]
    public Guid ApplicationId { get; set; }
    public Guid InternshipId { get; set; }
    public Guid StudentId { get; set; }
    public InternshipApplicationStatus Status { get; set; }
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedBy { get; set; }

    public virtual InternshipGroup InternshipGroup { get; set; } = null!;
    public virtual Student Student { get; set; } = null!;
    public virtual EnterpriseUser? Reviewer { get; set; }
}
