
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace IOCv2.API.Configurations;

public static class SecurityConfig
{
    public static IServiceCollection AddSecurityConfig(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Configure CORS
        // If AllowedOrigins is null or empty, use local dev ports
        var allowedOriginsConfig = configuration["AllowedOrigins"];
        var allowedOrigins = !string.IsNullOrWhiteSpace(allowedOriginsConfig)
            ? allowedOriginsConfig.Split(',', StringSplitOptions.RemoveEmptyEntries)
            : new[] { "http://localhost:3000", "http://localhost:3001" };

        services.AddCors(options =>
        {
            options.AddPolicy("AllowReact", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials(); // Required for Cookie-based auth
            });
        });

        // 2. Configure JWT Authentication
        // Clear default claim mapping to use standard OIDC claims
        System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

        var jwtSection = configuration.GetSection("Jwt");
        var secretKey = jwtSection["SecretKey"];

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSection["Issuer"],
                ValidAudience = jwtSection["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!))
            };

            // Support reading token from Cookie if Header is missing
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    // Preference 1: Bearer token in Header (standard)
                    // Preference 2: accessToken in Cookies (for better web security)
                    if (!context.Request.Headers.ContainsKey("Authorization"))
                    {
                        var accessToken = context.Request.Cookies["accessToken"];
                        if (!string.IsNullOrEmpty(accessToken))
                        {
                            context.Token = accessToken;
                        }
                    }
                    return Task.CompletedTask;
                }
            };
        });

        return services;
    }
}
