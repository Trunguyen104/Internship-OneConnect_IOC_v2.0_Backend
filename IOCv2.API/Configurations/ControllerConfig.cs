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
         
        });
        return services;
    }
}