using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IOCv2.Domain.Enums;

namespace IOCv2.Domain.Entities
{
    public class AuditLog : BaseEntity
    {
        public Guid AuditLogId { get; set; }
        public AuditAction Action { get; set; }
        public string EntityType { get; set; } = null!;
        public Guid EntityId { get; set; }
        public Guid? PerformedById { get; set; }
        public virtual User? PerformedBy { get; set; }
        public string? Reason { get; set; }
        public string? Metadata { get; set; } // jsonb
    }
}
