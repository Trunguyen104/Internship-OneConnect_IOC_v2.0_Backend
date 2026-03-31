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

        // Two-Layer Status Model
        public VisibilityStatus VisibilityStatus { get; private set; }
        public OperationalStatus OperationalStatus { get; private set; }

        // TEMPORARY: kept for backward compat with handlers not yet updated
        public ProjectStatus? Status { get; private set; }

        public Guid? MentorId { get; private set; }
        public string ProjectCode { get; private set; } = string.Empty;
        public string Field { get; private set; } = string.Empty;
        public ProjectTemplate Template { get; private set; }
        public string Requirements { get; private set; } = string.Empty;
        public string? Deliverables { get; private set; }

        // AC-16: true khi InternshipGroup bị xóa và project bị tách ra (orphan)
        public bool IsOrphaned { get; private set; }

        // Computed property
        public bool IsEditable => OperationalStatus == OperationalStatus.Unstarted || OperationalStatus == OperationalStatus.Active;

        // Navigation Properties
        public virtual InternshipGroup? InternshipGroup { get; set; }
        public virtual ICollection<ProjectResources> ProjectResources { get; set; } = new List<ProjectResources>();
        public virtual ICollection<WorkItem> WorkItems { get; set; } = new List<WorkItem>();
        public virtual ICollection<Sprint> Sprints { get; set; } = new List<Sprint>();

        private Project() { }

        public static Project Create(
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
                ProjectId         = Guid.NewGuid(),
                ProjectName       = projectName,
                Description       = description,
                ProjectCode       = projectCode,
                Field             = field,
                Requirements      = requirements,
                Deliverables      = deliverables,
                Template          = template,
                MentorId          = mentorId,
                StartDate         = startDate,
                EndDate           = endDate,
                VisibilityStatus  = VisibilityStatus.Draft,
                OperationalStatus = OperationalStatus.Unstarted,
                Status            = null,
                CreatedAt         = DateTime.UtcNow
            };
        }

        public void Update(
            string? projectName,
            string? description,
            DateTime? startDate,
            DateTime? endDate,
            string? field = null,
            string? requirements = null,
            string? deliverables = null,
            ProjectTemplate? template = null)
        {
            if (projectName != null)   ProjectName  = projectName;
            Description = description;
            StartDate   = startDate;
            EndDate     = endDate;
            if (field != null)         Field        = field;
            if (requirements != null)  Requirements = requirements;
            Deliverables = deliverables;
            if (template.HasValue)     Template     = template.Value;
            UpdatedAt = DateTime.UtcNow;
        }

        // Visibility lifecycle methods
        public void Publish()
        {
            VisibilityStatus = VisibilityStatus.Published;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Unpublish()
        {
            VisibilityStatus = VisibilityStatus.Draft;
            UpdatedAt = DateTime.UtcNow;
        }

        // Operational lifecycle methods
        public void SetOperationalStatus(OperationalStatus status)
        {
            OperationalStatus = status;
            UpdatedAt = DateTime.UtcNow;
        }

        public void AssignToGroup(Guid internshipId, DateTime? startDate, DateTime? endDate)
        {
            InternshipId      = internshipId;
            StartDate         = startDate;
            EndDate           = endDate;
            OperationalStatus = OperationalStatus.Active;
            IsOrphaned        = false;  // Reset orphan flag khi gán nhóm mới
            UpdatedAt         = DateTime.UtcNow;
        }

        public void SwapGroup(Guid newInternshipId, DateTime? startDate, DateTime? endDate)
        {
            InternshipId = newInternshipId;
            StartDate    = startDate;
            EndDate      = endDate;
            OperationalStatus = OperationalStatus.Active;
            IsOrphaned = false;
            UpdatedAt    = DateTime.UtcNow;
        }

        public void UnassignFromGroup()
        {
            InternshipId = null;
            StartDate = null;
            EndDate = null;
            OperationalStatus = OperationalStatus.Unstarted;
            IsOrphaned = false;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// AC-16: Khi InternshipGroup bị xóa, project bị "orphan" — tách khỏi group.
        /// InternshipId → null. OperationalStatus → Unstarted. VisibilityStatus không đổi.
        /// IsOrphaned → true (để phân biệt với project chưa bao giờ được gán nhóm).
        /// </summary>
        public void SetOrphan()
        {
            InternshipId      = null;
            OperationalStatus = OperationalStatus.Unstarted;
            IsOrphaned        = true;
            UpdatedAt         = DateTime.UtcNow;
        }

        // BACKWARD COMPAT: kept for handlers not yet migrated to two-layer status
        public void SetStatus(ProjectStatus newStatus)
        {
            Status    = newStatus;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
