using IOCv2.API.Hubs;

namespace IOCv2.API.Configurations;

public static class SignalRConfig
{
    public static IServiceCollection AddSignalRConfig(this IServiceCollection services)
    {
        services.AddSignalR();
        return services;
    }

    public static WebApplication UseSignalRConfig(this WebApplication app)
    {
        app.MapHub<NotificationHub>("/hubs/notifications");
        return app;
    }
}
