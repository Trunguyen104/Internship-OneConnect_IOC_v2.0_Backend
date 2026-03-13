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
        public ViolationStatus Status { get; set; }
        public ViolationType Type { get; set; }
        public ViolationSeverity Severity { get; set; }
        // Navigation properties
        public virtual ICollection<ViolationAttachment>? Attachments { get; set; }
        public virtual ICollection<ViolationComment>? Comments { get; set; }
        public virtual ICollection<ViolationUpdateHistory>? UpdateHistories { get; set; }
        public virtual Student Student { get; set; } = null!;
        public virtual InternshipGroup InternshipGroup { get; set; } = null!;
    }
}
