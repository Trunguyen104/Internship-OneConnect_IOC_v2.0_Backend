
using Microsoft.AspNetCore.HttpOverrides;

namespace IOCv2.API.Configurations;

public static class ForwardedHeadersConfig
{
    public static void AddForwardedHeadersConfig(this IServiceCollection services)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        });
    }
}
