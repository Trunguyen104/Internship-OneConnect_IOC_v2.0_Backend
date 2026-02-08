using IOCv2.API.Configurations;
using IOCv2.API.Middlewares;
using IOCv2.Application;
using IOCv2.Infrastructure;

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

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerConfig();

    // Database Migration & Seeding
    await DatabaseConfig.ApplyMigrations(app);
}

app.UseCors("AllowReact");

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
