using IOCv2.Domain.Enums;
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

        public class CommonUniAssignParam
        {
            private static readonly List<InternshipApplicationStatus> _allowedStatuses = new List<InternshipApplicationStatus>
            {
                InternshipApplicationStatus.Placed,
                InternshipApplicationStatus.PendingAssignment,
                InternshipApplicationStatus.Offered,
                InternshipApplicationStatus.Interviewing,
                InternshipApplicationStatus.Applied,
                InternshipApplicationStatus.Rejected,
            };
            public static List<InternshipApplicationStatus> AllowedStatuses => _allowedStatuses;
        }
    }
}
