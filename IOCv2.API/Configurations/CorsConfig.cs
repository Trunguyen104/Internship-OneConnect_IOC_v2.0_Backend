
using Microsoft.Extensions.DependencyInjection;

namespace IOCv2.API.Configurations;

public static class CorsConfig
{
    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration config)
    {
        //Setting for CORS - Load origins from configuration
        var corsOrigins = config["AllowedOrigins"]?.Split(',')
            ?? new[] { "http://localhost:3000" };

        services.AddCors(opt =>
        {
            opt.AddPolicy("AllowReact",
                policy => policy
                    .WithOrigins(corsOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials()); // Allow Cookies
        });
        return services;
    }
}
