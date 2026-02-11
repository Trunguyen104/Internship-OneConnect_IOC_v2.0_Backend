using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Domain.Enums
{
    public enum AuditAction : short
    {
        Create = 1,
        Update = 2,
        Deactivate = 3,
        Activate = 4,
        ResetPassword = 5,
        ChangeRole = 6,
        EmailFailure = 7
    }
}
