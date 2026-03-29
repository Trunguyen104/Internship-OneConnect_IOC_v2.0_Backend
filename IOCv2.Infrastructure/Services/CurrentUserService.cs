using IOCv2.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace IOCv2.Infrastructure.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string? UserId => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        public string? UserCode => _httpContextAccessor.HttpContext?.User?.FindFirstValue("UserCode");
        public string? Role => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Role);
        public string? IpAddress => _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
        public string? UnitId => _httpContextAccessor.HttpContext?.User?.FindFirstValue("UnitId");
    }
}
