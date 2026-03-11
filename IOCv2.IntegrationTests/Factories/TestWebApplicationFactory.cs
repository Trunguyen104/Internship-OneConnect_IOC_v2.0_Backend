using System.Data.Common;
using IOCv2.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IOCv2.IntegrationTests.Factories;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove ALL Entity Framework Core and Npgsql related registrations
            // This is required when moving from real Postgres to InMemory because EF internal DI cache
            var efDescriptors = services.Where(d => 
                (d.ServiceType.Namespace != null && d.ServiceType.Namespace.StartsWith("Microsoft.EntityFrameworkCore")) ||
                (d.ServiceType.Namespace != null && d.ServiceType.Namespace.StartsWith("Npgsql.EntityFrameworkCore")) ||
                d.ServiceType == typeof(AppDbContext) ||
                d.ServiceType == typeof(DbContextOptions) ||
                d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                d.ServiceType == typeof(DbConnection)).ToList();

            foreach (var descriptor in efDescriptors)
            {
                services.Remove(descriptor);
            }

            // Add AppDbContext using an in-memory database for testing.
            // Alternatively, you can use Testcontainers for a real database (e.g., PostgreSQL).
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase("InMemoryDbForTesting");
                options.ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
            });

            // If you use Redis or other external services, you can mock or disable them here
            // e.g. services.Remove(...) for Redis Cache
            var rateLimiterDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IOCv2.Application.Interfaces.IRateLimiter));
            if (rateLimiterDescriptor != null) services.Remove(rateLimiterDescriptor);

            var cacheServiceDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IOCv2.Application.Interfaces.ICacheService));
            if (cacheServiceDescriptor != null) services.Remove(cacheServiceDescriptor);

            var connectionMultiplexerDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(StackExchange.Redis.IConnectionMultiplexer));
            if (connectionMultiplexerDescriptor != null) services.Remove(connectionMultiplexerDescriptor);

            services.AddScoped<IOCv2.Application.Interfaces.IRateLimiter>(sp => new MockRateLimiter());
            services.AddScoped<IOCv2.Application.Interfaces.ICacheService>(sp => new MockCacheService());

            var sp = services.BuildServiceProvider();

            // Ensure the in-memory database is created.
            using (var scope = sp.CreateScope())
            {
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<AppDbContext>();
                
                // You can also grab password service from DI
                var passwordService = scopedServices.GetRequiredService<IOCv2.Application.Interfaces.IPasswordService>();

                db.Database.EnsureCreated();

                // Seed the database with test data here if necessary
                TestDbSeeder.SeedTestData(db, passwordService);
            }
        });

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole(); // log ra console để xem chi tiết exception
            logging.SetMinimumLevel(LogLevel.Debug);
        });
    }
public class MockRateLimiter : IOCv2.Application.Interfaces.IRateLimiter
{
    public Task<bool> IsBlockedAsync(string key, CancellationToken ct) => Task.FromResult(false);
    public Task<int> RegisterFailAsync(string key, int limit, TimeSpan window, TimeSpan blockFor, CancellationToken ct) => Task.FromResult(0);
    public Task ResetAsync(string key, CancellationToken ct) => Task.CompletedTask;
}

public class MockCacheService : IOCv2.Application.Interfaces.ICacheService
{
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) => Task.FromResult(default(T));
    public Task SetAsync<T>(string key, T value, TimeSpan? slidingExpiration = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task RemoveAsync(string key, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task RemoveByPrefixAsync(string prefixKey, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default) => Task.FromResult(false);
    public Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
}
