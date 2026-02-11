using IOCv2.Domain.Enums;

namespace IOCv2.Domain.Entities
{
    public class User : BaseEntity
    {
        public string Username { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string? PhoneNumber { get; private set; }
        public string? AvatarUrl { get; private set; }

        public DateOnly? DateOfBirth { get; set; }

        public UserStatus Status { get; set; }

        public UserRole Role { get; set; }
        public virtual ICollection<AuditLog> TargetLogs { get; set; } = new List<AuditLog>();
        public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
        public virtual ICollection<AuditLog> PeformedLogs { get; set; } = new List<AuditLog>();

    }
}
