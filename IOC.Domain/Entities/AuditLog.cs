using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOC.Domain.Entities
{
    public class AuditLog
    {
        public Guid Id { get; private set; }
        public string Action { get; private set; }
        public Guid TargetId { get; private set; }
        public string Description { get; private set; }
        public DateTime CreatedAt { get; private set; }

        private AuditLog() { }

        public static AuditLog Create(
            string action,
            Guid targetId,
            string description)
        {
            return new AuditLog
            {
                Id = Guid.NewGuid(),
                Action = action,
                TargetId = targetId,
                Description = description,
                CreatedAt = DateTime.UtcNow
            };
        }
    }

}
