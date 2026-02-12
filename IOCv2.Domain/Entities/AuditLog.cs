using IOCv2.Domain.Enums;
namespace IOCv2.Domain.Entities
{
    public class AuditLog
    {
        public Guid LogId { get; set; }
        public AuditAction Action { get; set; }
        public Guid TargetId { get; set; }
        public virtual User Target { get; set; } = null!;
        public Guid PerformedByEmployeeId { get; set; }
        public virtual User PerformedBy { get; set; } = null!;

        public string? Reason { get; set; }

        public string? Metadata { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
