using IOCv2.Domain.Enums;

namespace IOCv2.Domain.Entities;

public class ProjectMember : BaseEntity
{
    public Guid ProjectMemberId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid StudentId { get; set; }
    public ProjectMemberRole Role { get; set; }
    public MemberStatus Status { get; set; }

    // Navigation properties
    public virtual Project Project { get; set; } = null!;
    public virtual Student Student { get; set; } = null!;
    public virtual ICollection<WorkItem> AssignedWorkItems { get; set; } = new List<WorkItem>();
}
