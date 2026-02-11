using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IOCv2.Domain.Enums;

namespace IOCv2.Domain.Entities
{
    public class AuditLog
    {
        public Guid LogId { get; set; }
        public AuditAction Action { get; set; }
        public Guid TargetId { get; set; }
        public virtual User Target { get; set; } = null!;
        public Guid PerformedUserById { get; set; }
        public virtual User PerformedBy { get; set; } = null!;
        public string? Reason { get; set; }
        public string? Metadata { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
