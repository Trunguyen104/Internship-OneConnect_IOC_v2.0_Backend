using IOCv2.Domain.Enums;

namespace IOCv2.Domain.Entities
{
    public class Project : BaseEntity
    {
        public Guid ProjectId { get; private set; }
        public Guid InternshipId { get; private set; }
        public string ProjectName { get; private set; } = string.Empty;
        public string? Description { get; private set; }
        public DateTime? StartDate { get; private set; }
        public DateTime? EndDate { get; private set; }
        public ProjectStatus? Status { get; private set; }

        // Navigation Properties
        public virtual InternshipGroup InternshipGroup { get; set; } = null!;
        public virtual ICollection<ProjectResources> ProjectResources { get; set; } = new List<ProjectResources>();
        public virtual ICollection<Stakeholder> Stakeholders { get; set; } = new List<Stakeholder>();
        public virtual ICollection<WorkItem> WorkItems { get; set; } = new List<WorkItem>();
        public virtual ICollection<Sprint> Sprints { get; set; } = new List<Sprint>();

        private Project() { }

        public static Project Create(Guid internshipId, string projectName, string? description, DateTime? startDate = null, DateTime? endDate = null)
        {
            var project = new Project
            {
                ProjectId = Guid.NewGuid(),
                InternshipId = internshipId,
                ProjectName = projectName,
                Description = description,
                StartDate = startDate,
                EndDate = endDate,
                Status = ProjectStatus.Planning,
                CreatedAt = DateTime.UtcNow
            };
            return project;
        }

        public void Update(Guid? internshipId, string? projectName, string? description, DateTime? startDate, DateTime? endDate, ProjectStatus? status)
        {
            if (internshipId.HasValue && internshipId != Guid.Empty) InternshipId = internshipId.Value;
            if (projectName != null) ProjectName = projectName;
            Description = description;
            StartDate = startDate;
            EndDate = endDate;
            if (status.HasValue) Status = status.Value;
            
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
