using IOCv2.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Domain.Entities
{
    public class Term : BaseEntity
    {
        public Guid TermId { get; private set; }

        public Guid UniversityId { get; private set; }

        public string Name { get; private set; } = string.Empty;
        public DateTime? StartDate { get; private set; }
        public DateTime? EndDate { get; private set; }

        public TermStatus? Status { get; private set; }

        // Navigation
        public virtual University University { get; set; } = null!;
        public virtual ICollection<Internship> Internships { get; set; } = new List<Internship>();
    }
}
