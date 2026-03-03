using IOCv2.Domain.Enums;

namespace IOCv2.Domain.Entities
{
    public class Project : BaseEntity
    {
        public Guid ProjectId { get; set; }
        public Guid InternshipId { get; set; }
        public string? ProjectName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public ProjectStatus? Status { get; set; }

        // Navigation Properties
        public virtual InternshipGroup InternshipGroup { get; set; } = null!;
        public virtual ICollection<ProjectResources> ProjectResources { get; set; } = new List<ProjectResources>();
        public virtual ICollection<Stakeholder> Stakeholders { get; set; } = new List<Stakeholder>();
        public virtual ICollection<WorkItem> WorkItems { get; set; } = new List<WorkItem>();
        public virtual ICollection<Sprint> Sprints { get; set; } = new List<Sprint>();
        public virtual ICollection<Logbook> Logbooks { get; set; } = new List<Logbook>();

        public Project() { }

        public Project(Guid internshipId, string projectName, string? description)
        {
            ProjectId = Guid.NewGuid();
            InternshipId = internshipId;
            ProjectName = projectName;
            Description = description;
            Status = ProjectStatus.Planning;
        }
    }
}
