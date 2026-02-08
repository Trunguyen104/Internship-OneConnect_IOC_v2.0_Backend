using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Domain.Entities
{
    public class BaseEntity
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; } // database xu ly datatime nhanh hon boolean
        public string? UpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
    }
}
