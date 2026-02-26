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
        public Guid InternshipId { get; private set; }
        public Guid TermId { get; private set; }
        public Guid? EnterpriseId { get; private set; }
        public Guid MentorId { get; private set; }
        public DateTime? StartDate { get; private set; }
        public DateTime? EndDate { get; private set; }
        public InternshipGroupStatus Status { get; private set; }
        public virtual InternshipStudents InternshipStudents { get; private set; }
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
    }
}
