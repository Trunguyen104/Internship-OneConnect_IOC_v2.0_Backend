namespace IOCv2.Domain.Entities
{
    public class EnterpriseUser : BaseEntity
    {
        public Guid EnterpriseUserId { get; set; }

        public Guid EnterpriseId { get; set; }
        public virtual Enterprise Enterprise { get; set; } = null!;

        public Guid UserId { get; set; }
        public virtual User User { get; set; } = null!;

        public string? Position { get; set; }

        public virtual ICollection<InternshipGroup> MentoredGroups { get; set; } = new List<InternshipGroup>();
        public virtual ICollection<InternshipApplication> ReviewedApplications { get; set; } = new List<InternshipApplication>();
    }
}
