
namespace IOCv2.API.Configurations;

public static class LocalizationConfig
{
    public static IServiceCollection AddLocalizationConfig(this IServiceCollection services)
    {
        services.AddLocalization();
        return services;
    }

    public static WebApplication UseLocalizationConfig(this WebApplication app)
    {
        var supportedCultures = new[] { "vi", "en" };
        var localizationOptions = new RequestLocalizationOptions()
            .SetDefaultCulture("vi")
            .AddSupportedCultures(supportedCultures)
            .AddSupportedUICultures(supportedCultures);

        app.UseRequestLocalization(localizationOptions);
        return app;
    }
}
