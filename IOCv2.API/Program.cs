using IOCv2.API;
using IOCv2.Application;
using IOCv2.Application.Interfaces;
using IOCv2.Infrastructure;
using IOCv2.Infrastructure.BackgroundJobs;
using IOCv2.Infrastructure.Persistence;
using IOCv2.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using System.Text;

System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
var builder = WebApplication.CreateBuilder(args);

// ========================================
// Load Environment Variables from .env file
// ========================================
var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
if (File.Exists(envPath))
{
    DotNetEnv.Env.Load(envPath);
}

var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
if (!string.IsNullOrEmpty(jwtSecret))
{
    builder.Configuration["Jwt:SecretKey"] = jwtSecret;
}

var jwtAccessExpires = Environment.GetEnvironmentVariable("JWT_ACCESS_TOKEN_EXPIRES_IN_MINUTE");
if (!string.IsNullOrEmpty(jwtAccessExpires))
{
    builder.Configuration["Jwt:ExpiresInMinute"] = jwtAccessExpires;
}

var jwtRefreshExpires = Environment.GetEnvironmentVariable("JWT_REFRESH_TOKEN_EXPIRES_IN_DAYS");
if (!string.IsNullOrEmpty(jwtRefreshExpires))
{
    builder.Configuration["Jwt:RefreshTokenExpiresInDays"] = jwtRefreshExpires;
}

var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
var dbPort = Environment.GetEnvironmentVariable("DB_PORT");
var dbName = Environment.GetEnvironmentVariable("DB_NAME");
var dbUser = Environment.GetEnvironmentVariable("DB_USER");
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");

if (!string.IsNullOrEmpty(dbHost) && !string.IsNullOrEmpty(dbPassword))
{
    var connectionString = $"Host={dbHost};Port={dbPort ?? "5432"};Database={dbName ?? "IOCv2"};Username={dbUser ?? "postgres"};Password={dbPassword}";
    builder.Configuration["ConnectionStrings:DefaultConnection"] = connectionString;
}

var redisConnection = Environment.GetEnvironmentVariable("REDIS_CONNECTION");
if (!string.IsNullOrEmpty(redisConnection))
{
    builder.Configuration["ConnectionStrings:Redis"] = redisConnection;
}

var emailSmtpHost = Environment.GetEnvironmentVariable("EMAIL_SMTP_HOST");
var emailSmtpPort = Environment.GetEnvironmentVariable("EMAIL_SMTP_PORT");
var emailSenderEmail = Environment.GetEnvironmentVariable("EMAIL_SENDER_EMAIL");
var emailSenderName = Environment.GetEnvironmentVariable("EMAIL_SENDER_NAME");
var emailAppPassword = Environment.GetEnvironmentVariable("EMAIL_APP_PASSWORD");

if (!string.IsNullOrEmpty(emailSmtpHost))
    builder.Configuration["EmailSettings:SmtpHost"] = emailSmtpHost;
if (!string.IsNullOrEmpty(emailSmtpPort))
    builder.Configuration["EmailSettings:SmtpPort"] = emailSmtpPort;
if (!string.IsNullOrEmpty(emailSenderEmail))
    builder.Configuration["EmailSettings:SenderEmail"] = emailSenderEmail;
if (!string.IsNullOrEmpty(emailSenderName))
    builder.Configuration["EmailSettings:SenderName"] = emailSenderName;
if (!string.IsNullOrEmpty(emailAppPassword))
    builder.Configuration["EmailSettings:AppPassword"] = emailAppPassword;

var allowedOrigins = Environment.GetEnvironmentVariable("ALLOWED_ORIGINS");
if (!string.IsNullOrEmpty(allowedOrigins))
{
    builder.Configuration["AllowedOrigins"] = allowedOrigins;
}

builder.Services.AddStackExchangeRedisCache(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Redis")
                          ?? builder.Configuration["Redis:ConnectionString"]
                          ?? "localhost:6379";
    options.Configuration = connectionString;
    options.InstanceName = builder.Configuration["Redis:InstanceName"];
});

// Configure Forwarded Headers for Proxy/Load Balancer support
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

// Add services to the container.
builder.Services.AddControllers(opt =>
{
    opt.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "IOCv2 API", Version = "v1" });

    // Config JWT in Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

//Setting for CORS - Load origins from configuration
var corsOrigins = builder.Configuration["AllowedOrigins"]?.Split(',')
    ?? new[] { "http://localhost:3000" };

builder.Services.AddCors(opt =>
{
    opt.AddPolicy("AllowReact",
        policy => policy
            .WithOrigins(corsOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()); // Allow Cookies
});

//Setting for JWT Authentication
var jwt = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],

            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwt["SecretKey"]!))
        };
    });

// Register Layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Register Localization
builder.Services.AddLocalization();

var app = builder.Build();

// Configure Localization Middleware
var supportedCultures = new[] { "vi", "en" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture("vi")
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

app.UseRequestLocalization(localizationOptions);

app.UseForwardedHeaders();

app.UseMiddleware<ExceptionMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();

if (app.Environment.IsDevelopment())
{
    // Use Swashbuckle Swagger
    app.UseSwagger();
    app.UseSwaggerUI();

    // Auto Migrate & Seed Data
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var retryCount = 0;
        var maxRetries = 5;

        while (retryCount < maxRetries)
        {
            try
            {
                var context = services.GetRequiredService<AppDbContext>();
                var initializer = services.GetRequiredService<DbInitializer>();

                context.Database.Migrate();
                initializer.Initialize();
                break; // Success, exit loop
            }
            catch (Exception ex)
            {
                retryCount++;
                var logger = services.GetRequiredService<ILogger<Program>>();

                if (retryCount >= maxRetries)
                {
                    logger.LogError(ex, "An error occurred while migrating or seeding the database.");
                    throw;
                }

                logger.LogWarning("Database not ready. Retry {Count}/{Max}...", retryCount, maxRetries);
                await Task.Delay(2000);
            }
        }
    }
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
