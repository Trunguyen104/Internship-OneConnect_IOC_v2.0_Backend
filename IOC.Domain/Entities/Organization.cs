using IOC.Domain.Enums;
using IOC.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOC.Domain.Entities
{
    public class Organization
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public OrganizationType Type { get; private set; }
    }
}
