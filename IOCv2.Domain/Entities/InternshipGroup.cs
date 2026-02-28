using IOCv2.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Domain.Entities
{
    public class InternshipGroup : BaseEntity
    {
        public Guid InternshipId { get; set; }
        public Guid TermId { get; set; }
        public Guid? EnterpriseId { get; set; }
        public Guid MentorId { get; set; }

        public InternshipGroup(Guid internshipId, Guid termId, Guid? enterpriseId, Guid mentorId, DateTime? startDate, DateTime? endDate, InternshipGroupStatus status)
        {
            InternshipId = internshipId;
            TermId = termId;
            EnterpriseId = enterpriseId;
            MentorId = mentorId;
            StartDate = startDate;
            EndDate = endDate;
            Status = status;
        }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public InternshipGroupStatus Status { get; set; }

        public virtual Term Term { get; set; } = null!;
        public virtual Enterprise? Enterprise { get; set; }
        public virtual EnterpriseUser Mentor { get; set; } = null!;

        public virtual ICollection<InternshipStudents> InternshipStudents { get; set; } = new List<InternshipStudents>();
        public virtual ICollection<InternshipApplication> InternshipApplications { get; set; } = new List<InternshipApplication>();
        public virtual ICollection<Logbook> Logbooks { get; set; } = new List<Logbook>();
        public virtual ICollection<Project> Projects { get; set; } = new List<Project>();

        public InternshipGroup() { }
    }
}
