using IOCv2.Domain.Enums;

namespace IOCv2.Domain.Entities
{
    public class StakeholderIssue : BaseEntity
    {
        public Guid Id { get; set; }
        public Guid StakeholderId { get; set; }
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public StakeholderIssueStatus Status { get; set; } = StakeholderIssueStatus.Open;
        public DateTime? ResolvedAt { get; set; }

        public virtual Stakeholder Stakeholder { get; set; } = null!;
    }
}

