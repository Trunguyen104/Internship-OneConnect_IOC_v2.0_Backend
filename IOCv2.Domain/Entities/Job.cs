using IOCv2.Domain.Enums;

namespace IOCv2.Domain.Entities;

public class Job : BaseEntity
{
    public Guid JobId { get; set; }
    public Guid EnterpriseId { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string? Requirements { get; set; }
    public string? Location { get; set; }
    public string? Benefit { get; set; }
    public int Quantity { get; set; }
    public DateTime? ExpireDate { get; set; }
    public JobStatus Status { get; set; }

    // Navigation properties
    public virtual Enterprise Enterprise { get; set; } = null!;
    public virtual ICollection<InternshipApplication> Applications { get; set; } = new List<InternshipApplication>();
}
