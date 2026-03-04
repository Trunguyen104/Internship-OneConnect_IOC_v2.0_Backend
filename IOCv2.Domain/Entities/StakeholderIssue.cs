using IOCv2.Domain.Enums;

namespace IOCv2.Domain.Entities
{
    public class StakeholderIssue : BaseEntity
    {
        public Guid Id { get; private set; }
        public Guid StakeholderId { get; private set; }
        public string Title { get; private set; } = null!;
        public string Description { get; private set; } = null!;
        public StakeholderIssueStatus Status { get; private set; } = StakeholderIssueStatus.Open;
        public DateTime? ResolvedAt { get; private set; }

        public virtual Stakeholder Stakeholder { get; private set; } = null!;

        // Constructor for EF Core
        private StakeholderIssue() { }

        public StakeholderIssue(Guid id, Guid stakeholderId, string title, string description)
        {
            Id = id;
            StakeholderId = stakeholderId;
            Title = title;
            Description = description;
            Status = StakeholderIssueStatus.Open;
        }

        public void UpdateStatus(StakeholderIssueStatus status)
        {
            Status = status;

            if (status == StakeholderIssueStatus.Resolved || status == StakeholderIssueStatus.Closed)
            {
                ResolvedAt = DateTime.UtcNow;
            }
            else
            {
                ResolvedAt = null;
            }
        }
    }
}

