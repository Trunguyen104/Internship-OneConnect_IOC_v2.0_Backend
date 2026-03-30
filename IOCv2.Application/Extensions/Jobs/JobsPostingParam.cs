using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Extensions.Jobs
{
    public class JobsPostingParam
    {
        public class Common
        {
            public const int MinimumDurationDays = 28;      // 4 weeks
            public const int WarningThresholdDays = 42;     // 6 weeks  
            public const int MaximumDurationDays = 365;     // 12 months
        }
        public class Filter
        {
            public const string Desc = "desc";
            public const string Title = "title";
            public const string Position = "position";
            public const string Location = "location";
            public const string ExpireDate = "expiredate";
            public const string Status = "status";
        }

        public class GetJobPostings
        {
            public static readonly string[] EnterpriseRoles = new[] { "HR", "EnterpriseAdmin" };
            public static readonly string[] UniversityRoles = new[] { "Student" };
        }

        public class CreateJobPosting
        {
            public const string Draft = "DRAFT";
            public const string Published = "PUBLISHED";
        }
    }
}
