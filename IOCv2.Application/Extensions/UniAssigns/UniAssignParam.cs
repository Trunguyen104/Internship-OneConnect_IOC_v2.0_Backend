using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Extensions.UniAssigns
{
    public class UniAssignParam
    {
        public class CreateUniAssignParam
        {
            private static readonly string[] _uniAllowedRole = { "SchoolAdmin" };
            public static string[] UniAllowedRole => _uniAllowedRole;
        }
    }
}
