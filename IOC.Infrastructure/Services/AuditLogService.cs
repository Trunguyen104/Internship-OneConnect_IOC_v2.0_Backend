using IOC.Application.Commons.Interfaces.Services;
using IOC.Domain.Entities;
using IOC.Infrastructure.Persistences.DbContexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOC.Infrastructure.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly AppDbContext _context;

        public AuditLogService(AppDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(
            Guid actorId,
            string action,
            Guid targetId,
            string description,
            CancellationToken ct = default)
        {
            var log = AuditLog.Create(
                actorId,
                action,
                targetId,
                description);

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync(ct);
        }
    }
}
