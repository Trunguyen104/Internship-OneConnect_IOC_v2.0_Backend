using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.Authentication.Commands.Login
{
    public class LoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
        public double RefreshTokenExpiresIn { get; set; } // Duration in Seconds
        public int ExpiresIn { get; set; } // Access Token Duration in Seconds
    }
}
