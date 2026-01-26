using IOC.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOC.Domain.Extensions
{
    public static class AdminRoleExtensions
    {
        public static bool RequiresRole(this AdminRole role)
        {
            return role == AdminRole.School || role == AdminRole.Enterprise;
        }
    }

}
