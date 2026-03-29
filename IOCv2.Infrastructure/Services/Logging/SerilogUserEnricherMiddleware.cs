using Microsoft.AspNetCore.Http;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Infrastructure.Services.Logging
{
    public class SerilogUserEnricherMiddleware
    {
        private readonly RequestDelegate _next;

        public SerilogUserEnricherMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var ip = context.Connection?.RemoteIpAddress?.ToString();

            using (LogContext.PushProperty("UserId", userId))
            using (LogContext.PushProperty("IP", ip))
            using (LogContext.PushProperty("UserAgent", context.Request.Headers["User-Agent"].ToString()))
            {
                await _next(context);
            }
        }
    }

}
