using IOCv2.Domain.Enums;

namespace IOCv2.Domain.Entities;

public class Project : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Guid InternshipId { get; set; }
    public Guid? MentorId { get; set; }
    public string ProjectName { get; set; } = null!;
    public string Field { get; set; } = null!; // Web, AI...
    public string? Description { get; set; }
    public string? Tags { get; set; }
    public int ViewCount { get; set; } = 0;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public ProjectStatus Status { get; set; }

    // Navigation properties
    public virtual Internship Internship { get; set; } = null!;
    public virtual EnterpriseUser? Mentor { get; set; }
    public virtual ICollection<ProjectMember> Members { get; set; } = new List<ProjectMember>();
    public virtual ICollection<WorkItem> WorkItems { get; set; } = new List<WorkItem>();
}
