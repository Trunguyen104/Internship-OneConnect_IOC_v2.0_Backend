using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOC.Application.Commons.Interfaces.Services
{
    public interface IAuditLogService
    {
        Task LogAsync(
            string action,
            Guid targetId,
            string description,
            CancellationToken ct = default);
    }

}
