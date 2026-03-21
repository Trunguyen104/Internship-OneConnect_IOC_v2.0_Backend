using IOCv2.Domain.Enums;

namespace IOCv2.Domain.Entities;

public class StudentTerm : BaseEntity
{
    public Guid StudentTermId { get; set; }
    public Guid TermId { get; set; }
    public Guid StudentId { get; set; }
    public Guid? EnterpriseId { get; set; }

    public EnrollmentStatus EnrollmentStatus { get; set; } = EnrollmentStatus.Active;
    public PlacementStatus PlacementStatus { get; set; } = PlacementStatus.Unplaced;

    public DateOnly EnrollmentDate { get; set; }
    public string? EnrollmentNote { get; set; }
    public string? MidtermFeedback { get; set; }
    public string? FinalFeedback { get; set; }

    public Guid? DeletedBy { get; set; }

    public virtual Term Term { get; set; } = null!;
    public virtual Student Student { get; set; } = null!;
    public virtual Enterprise? Enterprise { get; set; }
}
