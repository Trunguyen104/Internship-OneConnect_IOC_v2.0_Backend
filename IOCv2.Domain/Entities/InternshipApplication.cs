using IOCv2.Domain.Enums;

namespace IOCv2.Domain.Entities;

public class InternshipApplication : BaseEntity
{
    public Guid ApplicationId { get; set; }
    public Guid EnterpriseId { get; set; }
    public Guid TermId { get; set; }
    public Guid StudentId { get; set; }

    public InternshipApplicationStatus Status { get; set; }
    public ApplicationSource Source { get; set; }

    /// <summary>Reject reason (required when Status = Rejected).</summary>
    public string? RejectReason { get; set; }

    /// <summary>Snapshot của tên Job Posting tại thời điểm apply (Self-apply only).</summary>
    public string? JobPostingTitle { get; set; }

    /// <summary>URL CV tại thời điểm apply (Self-apply only).</summary>
    public string? CvSnapshotUrl { get; set; }

    /// <summary>University chỉ định (Uni Assign only).</summary>
    public Guid? UniversityId { get; set; }

    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedBy { get; set; }

    // Navigation properties
    public virtual Enterprise Enterprise { get; set; } = null!;
    public virtual Term Term { get; set; } = null!;
    public virtual Student Student { get; set; } = null!;
    public virtual EnterpriseUser? Reviewer { get; set; }
    public virtual University? University { get; set; }
    public virtual ICollection<ApplicationStatusHistory> StatusHistories { get; set; } = new List<ApplicationStatusHistory>();

    /// <summary>Kiểm tra application có đang ở active stage hay không.</summary>
    public bool IsActive() => Status is
        InternshipApplicationStatus.Applied or
        InternshipApplicationStatus.Interviewing or
        InternshipApplicationStatus.Offered or
        InternshipApplicationStatus.PendingAssignment;
}
