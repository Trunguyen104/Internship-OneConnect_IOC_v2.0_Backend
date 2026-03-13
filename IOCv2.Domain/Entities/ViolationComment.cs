using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Domain.Entities
{
    public class ViolationComment : BaseEntity
    {
        public Guid Id { get; set; }
        public Guid ViolationReportId { get; set; }
        public Guid UserId { get; set; }
        public string Content { get; set; } = string.Empty!;
        public virtual ViolationReport ViolationReport { get; set; } = null!;
    }
}
