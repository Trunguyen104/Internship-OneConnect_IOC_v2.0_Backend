using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Domain.Entities
{
    public class RefreshToken : BaseEntity
    {
        public string Token { get; set; } = string.Empty;
        public DateTime Expires { get; set; }
        public bool IsRevoked { get; set; }
        public Guid EmployeeId { get; set; }
        public virtual User User { get; set; } = null!;
    }
}
