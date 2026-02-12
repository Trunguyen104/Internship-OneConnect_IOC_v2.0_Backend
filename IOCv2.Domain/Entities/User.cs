using IOCv2.Domain.Enums;

namespace IOCv2.Domain.Entities
{
    public class User : BaseEntity
    {
        public Guid UserId { get; set; }
        public string UserCode { get; set; } = null!;
        public string Username { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string? PhoneNumber { get; private set; }
        public string? AvatarUrl { get; private set; }
        public DateOnly? DateOfBirth { get; set; }
        public UserGender Gender { get; set; }
        public UserStatus Status { get; set; } = UserStatus.Active;
        public UserRole Role { get; set; }
        public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
        public virtual ICollection<AuditLog> TargetLogs { get; set; } = new List<AuditLog>();
        public virtual ICollection<AuditLog> PerformedLogs { get; set; } = new List<AuditLog>();
        public User() { }
    }
}
