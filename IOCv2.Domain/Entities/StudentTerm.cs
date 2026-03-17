using IOCv2.Domain.Enums;

namespace IOCv2.Domain.Entities;

public class StudentTerm : BaseEntity
{
    public Guid StudentTermId { get; set; }

    // Foreign keys
    public Guid TermId { get; set; }
    public Guid StudentId { get; set; }
    public Guid? EnterpriseId { get; set; }

    // Status
    public EnrollmentStatus EnrollmentStatus { get; set; } = EnrollmentStatus.Active;
    public PlacementStatus PlacementStatus { get; set; } = PlacementStatus.Unplaced;

    // Enrollment details
    public DateOnly EnrollmentDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public string? EnrollmentNote { get; set; }

    // Feedback (set by Enterprise, read-only for Admin)
    public string? MidtermFeedback { get; set; }
    public string? FinalFeedback { get; set; }

    // Navigation properties
    public virtual Term Term { get; set; } = null!;
    public virtual Student Student { get; set; } = null!;
    public virtual Enterprise? Enterprise { get; set; }
}
