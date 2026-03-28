using IOCv2.API.Configurations;
using IOCv2.API.Middlewares;
using IOCv2.API.Services;
using IOCv2.Application;
using IOCv2.Application.Interfaces;
using IOCv2.Infrastructure;
using IOCv2.Infrastructure.Services.Logging;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Load Environment Variables
builder.LoadEnvironmentVariables();

// Configure Serilog
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
});

// Add Core Services
builder.Services.AddControllerConfig();
builder.Services.AddVersioningConfig();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Add Infrastructure & Application Layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Add API Configurations
builder.Services.AddSwaggerConfig();
builder.Services.AddSecurityConfig(builder.Configuration);
builder.Services.AddRedisConfig(builder.Configuration);
builder.Services.AddForwardedHeadersConfig();
builder.Services.AddLocalizationConfig();
builder.Services.AddSignalRConfig();
builder.Services.AddHealthChecksConfig(builder.Configuration);

// Register API-layer services (depend on SignalR Hub, cannot go in Infrastructure)
builder.Services.AddScoped<INotificationPushService, SignalRNotificationPushService>();

var app = builder.Build();

// Configure Middleware Pipeline
// /health phải map TRƯỚC các middleware logic (auth, rate limit, etc.)
app.UseHealthChecksConfig();

app.UseLocalizationConfig();
app.UseForwardedHeaders();

app.UseExceptionHandler();
app.UseMiddleware<RateLimitingMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();

// Database Migration & Seeding — chạy mọi môi trường (kể cả Production)
await DatabaseConfig.ApplyMigrations(app);

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerConfig();
}
// Redirect / → /swagger chỉ trên Development
if (app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        if (context.Request.Path == "/")
        {
            context.Response.Redirect("/swagger");
            return;
        }

        await next();
    });
}
app.UseCors("AllowReact");

// KHÔNG dùng UseHttpsRedirection — backend chạy sau nginx proxy (HTTP nội bộ)
// nginx đã xử lý SSL termination. Redirect HTTPS ở đây sẽ gây vòng lặp 301.

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<SerilogUserEnricherMiddleware>();
app.UseSerilogRequestLogging();
var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "Uploads");
Directory.CreateDirectory(uploadsPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});
app.MapControllers();
app.UseSignalRConfig();

app.Run();

public partial class Program { }
