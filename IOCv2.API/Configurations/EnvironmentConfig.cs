
using DotNetEnv;
using Serilog;

namespace IOCv2.API.Configurations;

public static class EnvironmentConfig
{
    public static void LoadEnvironmentVariables(this WebApplicationBuilder builder)
    {
        var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
        if (File.Exists(envPath))
        {
            Env.Load(envPath);
        }

        // Logging 
        builder.Host.UseSerilog((context, services, configuration) =>
        {
            configuration.ReadFrom.Configuration(context.Configuration);
        });
        // Map JWT
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

        // Map Database
        var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
        var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");
        if (!string.IsNullOrEmpty(dbHost) && !string.IsNullOrEmpty(dbPassword))
        {
            var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
            var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "IOCv2";
            var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "postgres";
            builder.Configuration["ConnectionStrings:DefaultConnection"] = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword}";
        }

        // Map Redis & Others
        var redisConnection = Environment.GetEnvironmentVariable("REDIS_CONNECTION");
        if (!string.IsNullOrEmpty(redisConnection))
        {
            builder.Configuration["ConnectionStrings:Redis"] = redisConnection;
        }
        var allowedOrigins = Environment.GetEnvironmentVariable("ALLOWED_ORIGINS");
        if (!string.IsNullOrEmpty(allowedOrigins))
        {
            builder.Configuration["AllowedOrigins"] = allowedOrigins;
        }
        // Map Email
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
    }
}
