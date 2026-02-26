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
        //public virtual Term Term { get; set; } = null!;
        public Guid EnterpriseId { get; set; }
        public virtual Enterprise Enterprise { get; set; } = null!;
        public Guid MentorId { get; set; }
        public virtual EnterpriseUser Mentor { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public short Status { get; set; }

        public virtual ICollection<Logbook> Logbooks { get; set; } = new List<Logbook>();
    }
}
