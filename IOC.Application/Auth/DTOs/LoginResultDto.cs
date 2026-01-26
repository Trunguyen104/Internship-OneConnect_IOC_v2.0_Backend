using System;

namespace IOC.Application.Auth.DTOs
{
    // DTO returned by the login command containing token and its expiry
    public class LoginResultDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }
}