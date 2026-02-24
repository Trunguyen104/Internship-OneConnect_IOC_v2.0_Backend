using IOCv2.Domain.Enums;

namespace IOCv2.Domain.Entities
{
    public class Project : BaseEntity
    {
        public Guid Id { get; set; }
        public Guid InternshipId { get; set; }
        public Guid? MentorId { get; set; }
        public string ProjectName { get; set; } = null!;
        public string? Description { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public ProjectStatus Status { get; set; } = ProjectStatus.Planning;

        public virtual ICollection<Stakeholder> Stakeholders { get; set; } = new List<Stakeholder>();
    }
}
