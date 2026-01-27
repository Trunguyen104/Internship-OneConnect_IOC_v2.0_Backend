
using IOC.Application.Commons.Interfaces.Services;
using IOC.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;
using System;
using System.Security.Claims;

namespace IOC.Infrastructure.Services
{
    // Reads current user info from HttpContext.User claims (scoped)
    public class CurrentUserService : ICurrentUserService
    {
        public Guid UserId { get; }
        public AdminRole Role { get; }
        public Guid? OrganizationId { get; }

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            var user = httpContextAccessor.HttpContext?.User;
            if (user == null || !user.Identity?.IsAuthenticated == true)
                return;

            // Parse user id (sub)
            var sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (Guid.TryParse(sub, out var uid))
                UserId = uid;

            // Parse role claim
            var roleClaim = user.FindFirst(ClaimTypes.Role)?.Value;
            if (!string.IsNullOrWhiteSpace(roleClaim) && Enum.TryParse<AdminRole>(roleClaim, out var parsedRole))
                Role = parsedRole;

            // Parse org claim (if present)
            var orgClaim = user.FindFirst("org")?.Value;
            if (!string.IsNullOrWhiteSpace(orgClaim) && Guid.TryParse(orgClaim, out var orgId))
                OrganizationId = orgId;
        }
    }
}