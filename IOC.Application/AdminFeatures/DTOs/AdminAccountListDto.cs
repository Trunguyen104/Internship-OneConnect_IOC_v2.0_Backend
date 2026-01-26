using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOC.Application.AdminFeatures.DTOs
{
    public class AdminAccountListDto
    {
        public int Stt { get; set; }
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string OrganizationName { get; set; }
        public string Role { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

}
