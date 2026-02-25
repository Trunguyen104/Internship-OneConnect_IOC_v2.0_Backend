using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Domain.Enums
{
    public enum InternshipStatus : short
    {
        Registered = 0,
        Onboarded = 1,
        InProgress = 2,
        Completed = 3,
        Failed = 4
    }

}
