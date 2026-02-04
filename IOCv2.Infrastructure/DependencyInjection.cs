using IOCv2.Application.Interfaces;
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

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseNpgsql(
                    configuration.GetConnectionString("DefaultConnection"),
                    npgsqlOptions =>
                    {
                        npgsqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                    })
                .UseSnakeCaseNamingConvention();
            });
            services.AddHttpContextAccessor();

            services.AddScoped<ICurrentUserService, CurrentUserService>();

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
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<ITokenService, JwtTokenService>();
            services.AddScoped<DbInitializer>();

            // Rate Limiting Service
            services.AddScoped<IRateLimiter, RedisRateLimiter>();

            // Cache Service
            services.AddScoped<ICacheService, RedisCacheService>();

            return services;
        }
    }
}
