using IOCv2.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace IOCv2.API.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RateLimitAttribute : Attribute, IAsyncActionFilter
    {
        private readonly int _maxRequests;
        private readonly int _windowMinutes;
        private readonly int _blockMinutes;

        public RateLimitAttribute(
            int maxRequests = 100,
            int windowMinutes = 1,
            int blockMinutes = 5)
        {
            _maxRequests = maxRequests;
            _windowMinutes = windowMinutes;
            _blockMinutes = blockMinutes;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var rateLimiter = context.HttpContext.RequestServices.GetService<IRateLimiter>();
            if (rateLimiter == null)
            {
                await next();
                return;
            }

            var clientId = GetClientIdentifer(context.HttpContext);
            var endpoint = $"{context.HttpContext.Request.Method}:{context.HttpContext.Request.Path}";
            var rateLimitKey = $"rateLimit:{endpoint}:{clientId}";

            if (await rateLimiter.IsBlockedAsync(rateLimitKey, CancellationToken.None))
            {
                context.Result = new ObjectResult(new
                {
                    error = "Too many requests. Please try again later.",
                    retryAfter = _blockMinutes * 60
                })
                {
                    StatusCode = 429
                };
                context.HttpContext.Response.Headers.Append("Retry-After", (_blockMinutes * 60).ToString());
                return;
            }

            // Count EVERY request (success or failure) to enforce the rate limit correctly
            await rateLimiter.RegisterFailAsync(
                rateLimitKey,
                _maxRequests,
                TimeSpan.FromMinutes(_windowMinutes),
                TimeSpan.FromMinutes(_blockMinutes),
                CancellationToken.None);

            await next();
        }

        private string GetClientIdentifer(HttpContext context)
        {
            var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            var ipAdress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            return string.IsNullOrEmpty(userId) ? ipAdress : $"{userId}:{ipAdress}";
        }
    }
}
