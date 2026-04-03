
using DotNetEnv;
using Serilog;

namespace IOCv2.API.Configurations;

public static class EnvironmentConfig
{
    public static void LoadEnvironmentVariables(this WebApplicationBuilder builder)
    {
        // Walk up parent directories to find the shared root .env file
        var envPath = FindEnvFile(Directory.GetCurrentDirectory());

        if (envPath != null)
        {
            Env.Load(envPath);
        }

        // Map JWT
        var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
        if (!string.IsNullOrEmpty(jwtSecret))
            builder.Configuration["Jwt:SecretKey"] = jwtSecret;

        var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER");
        if (!string.IsNullOrEmpty(jwtIssuer))
            builder.Configuration["Jwt:Issuer"] = jwtIssuer;

        var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE");
        if (!string.IsNullOrEmpty(jwtAudience))
            builder.Configuration["Jwt:Audience"] = jwtAudience;

        var jwtAccessExpires = Environment.GetEnvironmentVariable("JWT_ACCESS_TOKEN_EXPIRES_IN_MINUTE");
        if (!string.IsNullOrEmpty(jwtAccessExpires))
            builder.Configuration["Jwt:ExpiresInMinute"] = jwtAccessExpires;

        var jwtRefreshExpires = Environment.GetEnvironmentVariable("JWT_REFRESH_TOKEN_EXPIRES_IN_DAYS");
        if (!string.IsNullOrEmpty(jwtRefreshExpires))
            builder.Configuration["Jwt:RefreshTokenExpiresInDays"] = jwtRefreshExpires;

        // Map Database
        var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
        var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");
        if (!string.IsNullOrEmpty(dbHost) && !string.IsNullOrEmpty(dbPassword))
        {
            var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
            var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "IOCV2Db";
            var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "postgres";
            
            var connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword}";
            builder.Configuration["ConnectionStrings:DefaultConnection"] = connectionString;
            
            Log.Information("Environment Variable Mapping: Database configured via environment variables. Port: {Port}", dbPort);
        }
        else
        {
            Log.Warning("Environment Variable Mapping: DB_HOST or DB_PASSWORD is missing. Using appsettings.json ConnectionStrings if provided.");
        }

        // Map Redis
        var redisConnection = Environment.GetEnvironmentVariable("REDIS_CONNECTION");
        if (!string.IsNullOrEmpty(redisConnection))
        {
            builder.Configuration["ConnectionStrings:Redis"] = redisConnection;
            builder.Configuration["Redis:ConnectionString"] = redisConnection;
            Log.Information("Environment Variable Mapping: Redis configured via environment variables.");
        }

        var redisInstance = Environment.GetEnvironmentVariable("REDIS_INSTANCE_NAME");
        if (!string.IsNullOrEmpty(redisInstance))
            builder.Configuration["Redis:InstanceName"] = redisInstance;

        // Map CORS
        var allowedOrigins = Environment.GetEnvironmentVariable("ALLOWED_ORIGINS");
        if (!string.IsNullOrEmpty(allowedOrigins))
            builder.Configuration["AllowedOrigins"] = allowedOrigins;

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
            
        // Map Logging
        var logLevelDefault = Environment.GetEnvironmentVariable("LOG_LEVEL_DEFAULT");
        if (!string.IsNullOrEmpty(logLevelDefault))
            builder.Configuration["Logging:LogLevel:Default"] = logLevelDefault;
            
        // Map Calendarific (Public Holiday API)
        var calendarificApiKey = Environment.GetEnvironmentVariable("CALENDARIFIC_API_KEY");
        if (!string.IsNullOrEmpty(calendarificApiKey))
        {
            builder.Configuration["Calendarific:ApiKey"] = calendarificApiKey;
            Log.Information("Environment Variable Mapping: Calendarific API Key configured via environment variables.");
        }

        Log.Information("Environment Variable Mapping: Completed.");
    }

    /// <summary>
    /// Walk up parent directories to find the nearest .env file.
    /// </summary>
    private static string? FindEnvFile(string startDir)
    {
        var dir = startDir;
        for (var i = 0; i < 5; i++) // max 5 levels up
        {
            var candidate = Path.Combine(dir, ".env");
            if (File.Exists(candidate)) return candidate;

            var parent = Directory.GetParent(dir);
            if (parent == null) break;
            dir = parent.FullName;
        }
        return null;
    }
}
