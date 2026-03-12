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

    public virtual University University { get; set; } = null!;
    public virtual ICollection<StudentTerm> StudentTerms { get; set; } = new List<StudentTerm>();
    public virtual ICollection<InternshipGroup> InternshipGroups { get; set; } = new List<InternshipGroup>();
}
