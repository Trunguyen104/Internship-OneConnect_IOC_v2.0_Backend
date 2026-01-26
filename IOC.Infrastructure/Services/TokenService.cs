using IOC.Application.Auth.DTOs;
using IOC.Application.Commons.Interfaces.Services;
using IOC.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace IOC.Infrastructure.Services
{
    // Generates JWT tokens using symmetric signing key from configuration.
    public class TokenService : ITokenService
    {
        private readonly string _key;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _expiresMinutes;

        public TokenService(IConfiguration configuration)
        {
            var section = configuration.GetSection("Jwt");
            _key = section.GetValue<string>("Key") ?? throw new ArgumentNullException("Jwt:Key");
            _issuer = section.GetValue<string>("Issuer") ?? string.Empty;
            _audience = section.GetValue<string>("Audience") ?? string.Empty;
            _expiresMinutes = section.GetValue<int>("ExpiresMinutes");
        }

        // Build token with basic claims: sub, email, role, organization (if any)
        public LoginResultDto GenerateToken(AdminAccount account)
        {
            var keyBytes = Encoding.UTF8.GetBytes(_key);
            var securityKey = new SymmetricSecurityKey(keyBytes);
            var creds = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var now = DateTime.UtcNow;
            var expires = now.AddMinutes(_expiresMinutes);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, account.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, account.Email.Value),
                new Claim(ClaimTypes.Role, account.Role.ToString()),
                new Claim("code", account.Code ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // Include organization id claim when account has organization
            if (account.OrganizationId.HasValue)
            {
                claims.Add(new Claim("org", account.OrganizationId.Value.ToString()));
            }

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                notBefore: now,
                expires: expires,
                signingCredentials: creds
            );

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenString = tokenHandler.WriteToken(token);

            return new LoginResultDto
            {
                AccessToken = tokenString,
                ExpiresAt = expires
            };
        }
    }
}