using IOCv2.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace IOCv2.Domain.Entities
{
    public class InternshipGroup : BaseEntity
    {
        [Key]
        public Guid InternshipId { get; private set; }
        public string GroupName { get; private set; } = string.Empty;
        public string? Description { get; private set; }

        public Guid PhaseId { get; private set; }
        public Guid? EnterpriseId { get; private set; }
        public virtual Enterprise? Enterprise { get; set; }

        public Guid? MentorId { get; private set; }
        public virtual EnterpriseUser? Mentor { get; set; }

        public DateTime? StartDate { get; private set; }
        public DateTime? EndDate { get; private set; }
        public GroupStatus Status { get; private set; }

        // Navigation properties
        public virtual InternshipPhase InternshipPhase { get; set; } = null!;
        private readonly List<InternshipStudent> _members = new();
        public virtual ICollection<InternshipStudent> Members => _members.AsReadOnly();
        
        public virtual ICollection<InternshipApplication> InternshipApplications { get; set; } = new List<InternshipApplication>();
        public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
        public virtual ICollection<Stakeholder> Stakeholders { get; set; } = new List<Stakeholder>();
        public virtual ICollection<Logbook> Logbooks { get; set; } = new List<Logbook>();
        public virtual ICollection<ViolationReport> ViolationReports { get; set; } = new List<ViolationReport>();

        protected InternshipGroup() { }

        public static InternshipGroup Create(
            Guid phaseId,
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
                PhaseId = phaseId,
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
            Guid phaseId,
            Guid? enterpriseId,
            Guid? mentorId,
            DateTime? startDate,
            DateTime? endDate)
        {
            GroupName = groupName;
            Description = description;
            PhaseId = phaseId;
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

        /// <summary>
        /// Chỉ thay đổi MentorId. Dùng cho quick-action assign/change mentor.
        /// Không thay đổi các trường khác của group.
        /// </summary>
        public void AssignMentor(Guid? newMentorEnterpriseUserId)
        {
            MentorId = newMentorEnterpriseUserId;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
