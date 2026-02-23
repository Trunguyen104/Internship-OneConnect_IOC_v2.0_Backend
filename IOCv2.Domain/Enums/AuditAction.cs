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
        Delete = 3,
        Approve = 4,
        Deactivate = 5,
        Activate = 6,
        ResetPassword = 7,
        ChangeRole = 8,
        EmailFailure = 9
    }
}
