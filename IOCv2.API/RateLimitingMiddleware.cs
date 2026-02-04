using IOCv2.Application.Interfaces;
using System.Net;

namespace IOCv2.API
{
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitingMiddleware> _logger;

        public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context, IRateLimiter rateLimiter)
        {
            // Lấy IP của client (đã xử lý qua Forwarded Headers nếu có Proxy)
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            
            // Kiểm tra IP có đang bị block không
            if (await rateLimiter.IsBlockedAsync(ipAddress, context.RequestAborted))
            {
                _logger.LogWarning("IP {IP} is blocked due to rate limiting.", ipAddress);
                
                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                context.Response.ContentType = "application/json";
                
                await context.Response.WriteAsJsonAsync(new 
                { 
                    status = (int)HttpStatusCode.TooManyRequests,
                    message = "Tài khoản hoặc IP của bạn đang bị tạm khóa do nhập sai nhiều lần. Vui lòng thử lại sau." 
                });
                return;
            }

            await _next(context);
        }
    }
}
