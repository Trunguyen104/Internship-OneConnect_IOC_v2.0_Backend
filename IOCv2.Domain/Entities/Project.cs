using IOCv2.Domain.Enums;

namespace IOCv2.Domain.Entities
{
    public class Project : BaseEntity
    {
        public Guid ProjectId { get; private set; }
        public Guid? InternshipId { get; private set; }   // NULLABLE — có thể là orphan
        public string ProjectName { get; private set; } = string.Empty;
        public string? Description { get; private set; }
        public DateTime? StartDate { get; private set; }
        public DateTime? EndDate { get; private set; }
        public ProjectStatus? Status { get; private set; }

        public Guid? MentorId { get; private set; }
        public string ProjectCode { get; private set; } = string.Empty;
        public string Field { get; private set; } = string.Empty;
        public ProjectTemplate Template { get; private set; }
        public string Requirements { get; private set; } = string.Empty;
        public string? Deliverables { get; private set; }

        // Navigation Properties — Assignments đã bị xóa
        public virtual InternshipGroup? InternshipGroup { get; set; }
        public virtual ICollection<ProjectResources> ProjectResources { get; set; } = new List<ProjectResources>();
        public virtual ICollection<WorkItem> WorkItems { get; set; } = new List<WorkItem>();
        public virtual ICollection<Sprint> Sprints { get; set; } = new List<Sprint>();

        private Project() { }

        public static Project Create(
            Guid? internshipId,          // NULLABLE
            string projectName,
            string? description,
            string projectCode,
            string field,
            string requirements,
            string? deliverables = null,
            ProjectTemplate template = ProjectTemplate.None,
            Guid? mentorId = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            return new Project
            {
                ProjectId    = Guid.NewGuid(),
                InternshipId = internshipId,
                ProjectName  = projectName,
                Description  = description,
                ProjectCode  = projectCode,
                Field        = field,
                Requirements = requirements,
                Deliverables = deliverables,
                Template     = template,
                MentorId     = mentorId,
                StartDate    = startDate,
                EndDate      = endDate,
                Status       = ProjectStatus.Draft,
                CreatedAt    = DateTime.UtcNow
            };
        }

        public void Update(
            Guid? internshipId,
            string? projectName,
            string? description,
            DateTime? startDate,
            DateTime? endDate,
            ProjectStatus? status,
            string? field = null,
            string? requirements = null,
            string? deliverables = null,
            ProjectTemplate? template = null)
        {
            if (internshipId.HasValue && internshipId != Guid.Empty) InternshipId = internshipId.Value;
            if (projectName != null)   ProjectName  = projectName;
            Description = description;
            StartDate   = startDate;
            EndDate     = endDate;
            if (status.HasValue)       Status       = status.Value;
            if (field != null)         Field        = field;
            if (requirements != null)  Requirements = requirements;
            Deliverables = deliverables;
            if (template.HasValue)     Template     = template.Value;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetStatus(ProjectStatus newStatus)
        {
            Status    = newStatus;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// AC-13: Khi InternshipGroup bị xóa, project bị "orphan" — tách khỏi group.
        /// Published → Draft. InternshipId → null.
        /// </summary>
        public void SetOrphan()
        {
            InternshipId = null;
            if (Status == ProjectStatus.Published)
                Status = ProjectStatus.Draft;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
