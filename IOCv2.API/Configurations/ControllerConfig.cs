using System.Text.Json.Serialization;

namespace IOCv2.API.Configurations;

public static class ControllerConfig
{
    public static IServiceCollection AddControllerConfig(this IServiceCollection services)
    {
        services.AddControllers(opt =>
        {
            opt.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
        })
        .AddJsonOptions(options =>
        {
            // By default, enums are serialized as integers. 
            // We keep it as integers according to the team communication contract.
            // options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        services.Configure<RouteOptions>(options =>
        {
            options.LowercaseUrls = true;
            options.LowercaseQueryStrings = true;
        });

        return services;
    }
}