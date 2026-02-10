using IOCv2.Domain.Enums;

namespace IOCv2.Domain.Entities;

public class StakeholderIssue : BaseEntity
{
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public StakeholderIssueStatus Status { get; set; } = StakeholderIssueStatus.Open;
    public Guid StakeholderId { get; set; }
    public DateTime? ResolvedAt { get; set; }
    
    // Navigation properties
    public virtual Stakeholder Stakeholder { get; set; } = null!;
}
