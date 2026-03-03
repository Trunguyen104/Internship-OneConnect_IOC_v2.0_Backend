using IOCv2.Domain.Enums;

namespace IOCv2.Domain.Entities
{
    public class InternshipGroup : BaseEntity
    {
        public Guid InternshipId { get; set; }
        public Guid TermId { get; set; }
        public string GroupName { get; set; } = string.Empty;

        public Guid? EnterpriseId { get; set; }
        public virtual Enterprise? Enterprise { get; set; }

        public Guid? MentorId { get; set; }
        public virtual EnterpriseUser? Mentor { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public InternshipStatus Status { get; set; }

        // Navigation properties
        public virtual Term Term { get; set; } = null!;
        public virtual ICollection<InternshipStudent> Members { get; set; } = new List<InternshipStudent>();
        public virtual ICollection<InternshipApplication> InternshipApplications { get; set; } = new List<InternshipApplication>();
        public virtual ICollection<Logbook> Logbooks { get; set; } = new List<Logbook>();
        public virtual ICollection<Project> Projects { get; set; } = new List<Project>();

        public InternshipGroup() { }
    }
}
