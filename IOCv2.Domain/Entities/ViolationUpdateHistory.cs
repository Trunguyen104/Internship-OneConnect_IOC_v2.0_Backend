using IOCv2.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Domain.Entities
{
    public class ViolationUpdateHistory : BaseEntity
    {
        public Guid Id { get; set; }
        public Guid ViolationReportId { get; set; }
        public ViolationStatus OldStatus { get; set; }
        public ViolationStatus NewStatus { get; set; }
        public string Reason { get; set; } = string.Empty!;

        public virtual ViolationReport ViolationReport { get; set; } = null!;
    }
}
