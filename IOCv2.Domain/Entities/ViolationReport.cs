using IOCv2.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Domain.Entities
{
    public class ViolationReport : BaseEntity
    {
        public Guid ViolationReportId { get; set; }
        public Guid StudentId { get; set; }
        public Guid InternshipGroupId { get; set; }
        public DateOnly OccurredDate { get; set; }
        public string Description { get; set; } = string.Empty!;
        // Navigation properties
        public virtual Student Student { get; set; } = null!;
        public virtual InternshipGroup InternshipGroup { get; set; } = null!;
    }
}