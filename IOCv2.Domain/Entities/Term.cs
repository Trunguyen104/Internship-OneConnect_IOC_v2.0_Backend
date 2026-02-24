using IOCv2.Domain.Enums;

namespace IOCv2.Domain.Entities;

public class Term : BaseEntity
{
    public Guid TermId { get; set; }
    public Guid UniversityId { get; set; }
    public string? Name { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int Status { get; set; }

    // Navigation properties
    public virtual University University { get; set; } = null!;
    public virtual ICollection<Internship> Internships { get; set; } = new List<Internship>();
}
