namespace IOCv2.Infrastructure.Security
{
    public class JwtSettings
    {
        public string SecretKey { get; set; } = null!;
        public string Issuer { get; set; } = null!;
        public string Audience { get; set; } = null!;
        public int ExpiresInMinute { get; set; }
        public int RefreshTokenExpiresInDays { get; set; }
    }
}
