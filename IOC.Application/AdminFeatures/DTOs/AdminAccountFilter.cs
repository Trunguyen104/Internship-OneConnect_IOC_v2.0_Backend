using IOC.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOC.Application.AdminFeatures.DTOs
{
    public class AdminAccountFilter
    {
        public string Keyword { get; set; }
        public string Email { get; set; }
        public string Code { get; set; }
        public Guid? OrganizationId { get; set; }
        public AdminRole? Role { get; set; }
        public AccountStatus? Status { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }

        public string SortBy { get; set; }
        public string SortDir { get; set; }

        public int PageIndex { get; set; }
        public int PageSize { get; set; }
    }

}
