using IOCv2.Domain.Enums;

namespace IOCv2.Domain.Entities
{
    public class User : BaseEntity
    {
        public Guid UserId { get; private set; }
        public string UserCode { get; private set; } = null!;
        public string PasswordHash { get; private set; } = null!;
        public string Email { get; private set; } = null!;
        public string FullName { get; private set; } = null!;
        public string? PhoneNumber { get; private set; }
        public string? AvatarUrl { get; private set; }
        public DateOnly? DateOfBirth { get; private set; }
        public UserGender Gender { get; private set; }
        public UserStatus Status { get; private set; } = UserStatus.Active;
        public UserRole Role { get; private set; }

        public virtual ICollection<RefreshToken> RefreshTokens { get; private set; } = new List<RefreshToken>();
        public virtual ICollection<AuditLog> PerformedLogs { get; private set; } = new List<AuditLog>();

        public virtual Student? Student { get; private set; }
        public virtual UniversityUser? UniversityUser { get; private set; }
        public virtual EnterpriseUser? EnterpriseUser { get; private set; }

        // Constructor for EF Core
        private User() { }

        public User(Guid userId, string userCode, string email, string fullName, UserRole role, string passwordHash)
        {
            UserId = userId;
            UserCode = userCode;
            Email = email;
            FullName = fullName;
            Role = role;
            PasswordHash = passwordHash;
            Status = UserStatus.Active;
        }

        public void UpdateProfile(string fullName, string? phoneNumber, string? avatarUrl, UserGender? gender, DateOnly? dateOfBirth)
        {
            FullName = fullName;
            if (phoneNumber != null) PhoneNumber = phoneNumber;
            if (avatarUrl != null) AvatarUrl = avatarUrl;
            if (gender.HasValue) Gender = gender.Value;
            if (dateOfBirth.HasValue) DateOfBirth = dateOfBirth.Value;
        }

        public void SetStatus(UserStatus status)
        {
            Status = status;
        }

        public void UpdatePassword(string passwordHash)
        {
            PasswordHash = passwordHash;
        }

        public void UpdateEmail(string email)
        {
            Email = email;
        }
    }
}
