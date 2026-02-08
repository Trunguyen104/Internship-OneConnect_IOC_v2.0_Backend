
using Microsoft.Extensions.Caching.StackExchangeRedis;

namespace IOCv2.API.Configurations;

public static class RedisConfig
{
    public static IServiceCollection AddRedisConfig(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddStackExchangeRedisCache(options =>
        {
             var connectionString = configuration.GetConnectionString("Redis")
                                   ?? configuration["Redis:ConnectionString"]
                                   ?? "localhost:6379";
             options.Configuration = connectionString;
             options.InstanceName = configuration["Redis:InstanceName"];
        });
        return services;
    }
}
