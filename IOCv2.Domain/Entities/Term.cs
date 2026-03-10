using IOCv2.Domain.Enums;

namespace IOCv2.Domain.Entities;

public class Term : BaseEntity
{
    public Guid TermId { get; set; }
    public Guid UniversityId { get; set; }
    public string Name { get; set; } = null!;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public TermStatus Status { get; set; }

    // Optimistic Locking
    public int Version { get; set; } = 1;

    // Denormalization: Counter to prevent N+1 Query for list
    public int TotalEnrolled { get; set; }
    public int TotalPlaced { get; set; }
    public int TotalUnplaced { get; set; }

    // Track who closed the term
    public Guid? ClosedBy { get; set; }
    public DateTime? ClosedAt { get; set; }

    // Track who deleted the term (Terms-specific field)
    public Guid? DeletedBy { get; set; }

    public virtual University University { get; set; } = null!;
    public virtual ICollection<StudentTerm> StudentTerms { get; set; } = new List<StudentTerm>();
    public virtual ICollection<InternshipGroup> InternshipGroups { get; set; } = new List<InternshipGroup>();
}