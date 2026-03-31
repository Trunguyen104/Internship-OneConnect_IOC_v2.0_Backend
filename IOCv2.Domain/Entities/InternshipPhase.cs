using IOCv2.Domain.Enums;

namespace IOCv2.Domain.Entities
{
    public class InternshipPhase : BaseEntity
    {
        public Guid PhaseId { get; private set; }
        public Guid EnterpriseId { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public DateOnly StartDate { get; private set; }
        public DateOnly EndDate { get; private set; }
        public string MajorFields { get; private set; } = string.Empty;
        public int Capacity { get; private set; }
        // Legacy compatibility for code paths not migrated yet.
        public int? MaxStudents => Capacity;
        public string? Description { get; private set; }
        public InternshipPhaseStatus Status { get; private set; } = InternshipPhaseStatus.Draft;

        // Navigation properties
        public virtual Enterprise? Enterprise { get; set; }
        public virtual ICollection<InternshipGroup> InternshipGroups { get; set; } = new List<InternshipGroup>();
        public virtual ICollection<EvaluationCycle> EvaluationCycles { get; set; } = new List<EvaluationCycle>();

        // Valid status transitions
        private static readonly Dictionary<InternshipPhaseStatus, InternshipPhaseStatus[]> _allowedTransitions = new()
        {
            [InternshipPhaseStatus.Draft] = new[] { InternshipPhaseStatus.Open },
            [InternshipPhaseStatus.Open] = new[] { InternshipPhaseStatus.InProgress, InternshipPhaseStatus.Closed },
            [InternshipPhaseStatus.InProgress] = new[] { InternshipPhaseStatus.Closed },
            [InternshipPhaseStatus.Closed] = Array.Empty<InternshipPhaseStatus>()
        };

        protected InternshipPhase() { }

        public static InternshipPhase Create(
            Guid enterpriseId,
            string name,
            DateOnly startDate,
            DateOnly endDate,
            string majorFields,
            int capacity,
            string? description)
        {
            return new InternshipPhase
            {
                PhaseId = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                Name = name,
                StartDate = startDate,
                EndDate = endDate,
                MajorFields = majorFields,
                Capacity = capacity,
                Description = description,
                Status = InternshipPhaseStatus.Draft
            };
        }

        // Legacy overload kept to avoid touching unrelated call sites.
        public static InternshipPhase Create(
            Guid enterpriseId,
            string name,
            DateOnly startDate,
            DateOnly endDate,
            int? maxStudents,
            string? description)
            => Create(enterpriseId, name, startDate, endDate, string.Empty, maxStudents ?? 1, description);

        public bool IsUpcoming(DateOnly today) => StartDate > today;

        public bool IsActive(DateOnly today) => StartDate <= today && EndDate >= today;

        public bool IsEnded(DateOnly today) => EndDate < today;

        /// <summary>
        /// Check if a status transition is allowed.
        /// </summary>
        public bool CanTransitionTo(InternshipPhaseStatus newStatus)
        {
            if (Status == InternshipPhaseStatus.Closed) return false; // Closed is terminal — no further transitions (checked first)
            if (Status == newStatus) return true; // no-op is allowed on non-Closed states
            return _allowedTransitions.TryGetValue(Status, out var allowed) && allowed.Contains(newStatus);
        }

        public void UpdateInfo(
            string name,
            DateOnly startDate,
            DateOnly endDate,
            string majorFields,
            int capacity,
            string? description,
            InternshipPhaseStatus status)
        {
            Name = name;
            StartDate = startDate;
            EndDate = endDate;
            MajorFields = majorFields;
            Capacity = capacity;
            Description = description;
            Status = status;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateInfo(
            string name,
            DateOnly startDate,
            DateOnly endDate,
            string majorFields,
            int capacity,
            string? description)
        {
            Name = name;
            StartDate = startDate;
            EndDate = endDate;
            MajorFields = majorFields;
            Capacity = capacity;
            Description = description;
            UpdatedAt = DateTime.UtcNow;
        }

        // Legacy overload kept for existing seed/init paths.
        public void UpdateInfo(
            string name,
            DateOnly startDate,
            DateOnly endDate,
            int? maxStudents,
            string? description,
            InternshipPhaseStatus status)
        {
            Name = name;
            StartDate = startDate;
            EndDate = endDate;
            Capacity = maxStudents ?? 1;
            Description = description;
            Status = status;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
