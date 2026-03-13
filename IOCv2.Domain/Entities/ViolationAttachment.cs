using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Domain.Entities
{
    public class ViolationAttachment : BaseEntity
    {
        public Guid Id { get; set; }
        public Guid ViolationReportId { get; set; }
        public string FilePath { get; set; } = string.Empty!;
        public string FileName { get; set; } = string.Empty!;
        // Navigation
        public virtual ViolationReport ViolationReport { get; set; } = null!;
    }
}
