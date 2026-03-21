using IOCv2.Domain.Enums;

namespace IOCv2.Domain.Entities
{
    public class InternshipGroup : BaseEntity
    {
        public Guid InternshipId { get; private set; }
        public Guid TermId { get; private set; }
        public string GroupName { get; private set; } = string.Empty;
        public string? Description { get; private set; }

        public Guid? EnterpriseId { get; private set; }
        public virtual Enterprise? Enterprise { get; set; }

        public Guid? MentorId { get; private set; }
        public virtual EnterpriseUser? Mentor { get; set; }

        public DateTime? StartDate { get; private set; }
        public DateTime? EndDate { get; private set; }
        public GroupStatus Status { get; private set; }

        // Navigation properties
        public virtual Term Term { get; set; } = null!;
        private readonly List<InternshipStudent> _members = new();
        public virtual ICollection<InternshipStudent> Members => _members.AsReadOnly();
        
        public virtual ICollection<InternshipApplication> InternshipApplications { get; set; } = new List<InternshipApplication>();
        public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
        public virtual ICollection<Stakeholder> Stakeholders { get; set; } = new List<Stakeholder>();
        public virtual ICollection<Logbook> Logbooks { get; set; } = new List<Logbook>();

        protected InternshipGroup() { }

        public static InternshipGroup Create(
            Guid termId,
            string groupName,
            string? description = null,
            Guid? enterpriseId = null,
            Guid? mentorId = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            return new InternshipGroup
            {
                InternshipId = Guid.NewGuid(),
                TermId = termId,
                GroupName = groupName,
                Description = description,
                EnterpriseId = enterpriseId,
                MentorId = mentorId,
                StartDate = startDate,
                EndDate = endDate,
                Status = GroupStatus.Active
            };
        }

        public void UpdateInfo(
            string groupName,
            string? description,
            Guid termId,
            Guid? enterpriseId,
            Guid? mentorId,
            DateTime? startDate,
            DateTime? endDate)
        {
            GroupName = groupName;
            Description = description;
            TermId = termId;
            EnterpriseId = enterpriseId;
            MentorId = mentorId;
            StartDate = startDate;
            EndDate = endDate;
        }

        public void AddMember(Guid studentId, InternshipRole role)
        {
            if (_members.Any(m => m.StudentId == studentId)) return;

            _members.Add(new InternshipStudent
            {
                StudentId = studentId,
                InternshipId = InternshipId,
                Role = role,
                Status = InternshipStatus.Registered,
                JoinedAt = DateTime.UtcNow
            });
        }

        public void RemoveMember(Guid studentId)
        {
            var member = _members.FirstOrDefault(m => m.StudentId == studentId);
            if (member != null)
            {
                _members.Remove(member);
            }
        }
        public void UpdateStatus(GroupStatus status)
        {
            Status = status;
        }
    }
}
