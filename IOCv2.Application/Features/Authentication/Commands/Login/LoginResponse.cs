using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.Authentication.Commands.Login
{
    /// <summary>
    /// Contains user identity details upon successful authentication. AccessToken and RefreshToken are handled via HttpOnly Cookies.
    /// </summary>
    public class LoginResponse
    {
        /// <summary>
        /// Short-lived JWT access token for API authorization. Excluded from JSON response to prevent XSS.
        /// </summary>
        //[System.Text.Json.Serialization.JsonIgnore]
        public string AccessToken { get; set; } = string.Empty;

        //[System.Text.Json.Serialization.JsonIgnore]
        public string RefreshToken { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public Guid? UnitId { get; set; }
        public double RefreshTokenExpiresIn { get; set; } // Duration in Seconds
        public int ExpiresIn { get; set; } // Access Token Duration in Seconds
    }
}
