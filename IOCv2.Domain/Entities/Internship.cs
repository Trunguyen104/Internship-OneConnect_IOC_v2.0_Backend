using IOCv2.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Domain.Entities
{
    public class Internship : BaseEntity
    {
        public Guid InternshipId { get; set; }

        public Guid TermId { get; set; }
        public Guid StudentId { get; set; }
        public Guid? JobId { get; set; }
        public Guid? MentorId { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public InternshipStatus? Status { get; set; }

        // Navigation
        public virtual Term Term { get; set; } = null!;
        public virtual Student Student { get; set; } = null!;
        public virtual EnterpriseUser Mentor { get; set; }
        public virtual ICollection<Project> Projects { get; set; } = new List<Project>();

    }
}
