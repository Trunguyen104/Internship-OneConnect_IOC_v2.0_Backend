
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;

namespace IOCv2.API.Configurations;

public static class HealthChecksConfig
{
    public static IServiceCollection AddHealthChecksConfig(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
        var redisConnection  = configuration.GetConnectionString("Redis")
                            ?? configuration["Redis:ConnectionString"]
                            ?? "localhost:6379";

        services.AddHealthChecks()
            .AddNpgSql(
                connectionString,
                name: "postgres",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["db", "infrastructure"])
            .AddRedis(
                redisConnection,
                name: "redis",
                failureStatus: HealthStatus.Degraded,
                tags: ["cache", "infrastructure"]);

        if (string.IsNullOrEmpty(connectionString))
        {
            // We use a dummy check or similar to avoid crashing if DB is intentionally not set,
            // but in this app it's required for health.
            // Logging is already handled in EnvironmentConfig.
        }

        return services;
    }

    /// <summary>
    /// Map /health endpoint với response JSON chi tiết.
    /// Docker healthcheck: wget -qO- http://localhost:8080/health
    /// </summary>
    public static void UseHealthChecksConfig(this WebApplication app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = WriteJsonResponse,
            // Chỉ trả về Healthy/Unhealthy, không leak thông tin chi tiết ra ngoài
            // trừ khi đang Development
            ResultStatusCodes =
            {
                [HealthStatus.Healthy]   = StatusCodes.Status200OK,
                [HealthStatus.Degraded]  = StatusCodes.Status200OK,   // Degraded vẫn pass healthcheck
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable,
            }
        });
    }

    private static async Task WriteJsonResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var isDev = context.RequestServices
            .GetRequiredService<IWebHostEnvironment>()
            .IsDevelopment();

        object response;

        if (isDev)
        {
            // Development: trả về chi tiết từng check
            response = new
            {
                status  = report.Status.ToString(),
                totalDuration = report.TotalDuration.TotalMilliseconds,
                checks = report.Entries.Select(e => new
                {
                    name     = e.Key,
                    status   = e.Value.Status.ToString(),
                    duration = e.Value.Duration.TotalMilliseconds,
                    description = e.Value.Description,
                    error    = e.Value.Exception?.Message
                })
            };
        }
        else
        {
            // Production: chỉ trả về status tổng thể (không leak nội bộ)
            response = new
            {
                status = report.Status.ToString()
            };
        }

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = false }));
    }
}
