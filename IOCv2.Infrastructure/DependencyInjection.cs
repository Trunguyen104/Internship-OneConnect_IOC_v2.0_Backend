using IOCv2.Application.Interfaces;
using IOCv2.Infrastructure.BackgroundJobs;
using IOCv2.Infrastructure.Persistence;
using IOCv2.Infrastructure.Persistence.Repositories;
using IOCv2.Infrastructure.Security;
using IOCv2.Infrastructure.Services;
using IOCv2.Infrastructure.Services.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace IOCv2.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
            services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));

            services.AddDbContextPool<AppDbContext>(options =>
            {
                options.UseNpgsql(
                    configuration.GetConnectionString("DefaultConnection"),
                    npgsqlOptions =>
                    {
                        npgsqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                    })
                .UseSnakeCaseNamingConvention()
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
            });
            services.AddHttpContextAccessor();

            services.AddSingleton<ICurrentUserService, CurrentUserService>();

            // Register Redis Connection
            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var redisConnectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379";
                var options = ConfigurationOptions.Parse(redisConnectionString);
                options.AbortOnConnectFail = false; // Allow app to start even if Redis is down
                return ConnectionMultiplexer.Connect(options);
            });

            // Register Services
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<IPasswordService, PasswordService>();
            // Register Background Email Channel as Singleton (must be shared)
            services.AddSingleton<BackgroundEmailChannel>();
            services.AddSingleton<IBackgroundEmailSender>(sp => sp.GetRequiredService<BackgroundEmailChannel>());

            // Register Hosted Service to process emails
            services.AddHostedService<EmailHostedService>();

            // Register Background Job to auto-close expired job postings
            services.AddHostedService<JobExpiryHostedService>();

            // Register Background Job to auto-complete expired projects
            services.AddHostedService<AutoCompleteProjectsJob>();

            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<ITokenService, JwtTokenService>();
            services.AddScoped<DbInitializer>();

            // Rate Limiting Service
            services.AddScoped<IRateLimiter, RedisRateLimiter>();

            // Cache Service
            services.AddScoped<ICacheService, RedisCacheService>();

            services.AddScoped<IOtpService, OtpService>();
            services.AddScoped<ILandingEmailPolicy, LandingEmailPolicy>();

            // Public Holiday External API
            services.AddScoped<IPublicHolidayApiService, CalendarificService>();

            // File
            services.AddHttpClient();

            var fileStorageProvider = configuration["FileStorage:Provider"]?.Trim().ToLowerInvariant();
            var cloudinaryCloudName = configuration["Cloudinary:CloudName"];
            var cloudinaryApiKey = configuration["Cloudinary:ApiKey"];
            var cloudinaryApiSecret = configuration["Cloudinary:ApiSecret"];
            var cloudinaryConfigured = !string.IsNullOrWhiteSpace(cloudinaryCloudName)
                                     && !string.IsNullOrWhiteSpace(cloudinaryApiKey)
                                     && !string.IsNullOrWhiteSpace(cloudinaryApiSecret);

            if (string.Equals(fileStorageProvider, "cloudinary", StringComparison.OrdinalIgnoreCase))
            {
                if (!cloudinaryConfigured)
                {
                    throw new InvalidOperationException("FileStorage Provider is set to Cloudinary but Cloudinary credentials are missing.");
                }

                services.AddScoped<IFileStorageService, CloudinaryFileStorageService>();
            }
            else if (cloudinaryConfigured && !string.Equals(fileStorageProvider, "local", StringComparison.OrdinalIgnoreCase))
            {
                services.AddScoped<IFileStorageService, CloudinaryFileStorageService>();
            }
            else
            {
                services.AddScoped<IFileStorageService, LocalFileStorageService>();
            }

            return services;
        }
    }
}
