using IOC.Domain.Enums;
using IOC.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOC.Application.AdminFeatures.DTOs
{
    public class AdminAccountUpdateDTO
    {
        public string FullName { get; private set; }
        public Email Email { get; private set; }
        public AdminRole Role { get; private set; }
        public Guid? OrganizationId { get; private set; }
        public string Code { get; private set; }

    }
}
