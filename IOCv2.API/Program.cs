using IOCv2.API.Configurations;
using IOCv2.API.Middlewares;
using IOCv2.Application;
using IOCv2.Infrastructure;
using IOCv2.Infrastructure.Services.Logging;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Load Environment Variables
builder.LoadEnvironmentVariables();

// Add Core Services
builder.Services.AddControllerConfig();

// Add Infrastructure & Application Layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Add API Configurations
builder.Services.AddSwaggerConfig();
builder.Services.AddCorsPolicy(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddRedisConfig(builder.Configuration);
builder.Services.AddForwardedHeadersConfig();
builder.Services.AddLocalizationConfig();

var app = builder.Build();

// Configure Middleware Pipeline
app.UseLocalizationConfig();
app.UseForwardedHeaders();

app.UseMiddleware<ExceptionMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<SerilogUserEnricherMiddleware>();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerConfig();

    // Database Migration & Seeding
    await DatabaseConfig.ApplyMigrations(app);
}
// redirect / → /swagger
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/")
    {
        context.Response.Redirect("/swagger");
        return;
    }

    await next();
});
app.UseCors("AllowReact");

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
