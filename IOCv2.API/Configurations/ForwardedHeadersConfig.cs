
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
            // Trust tất cả proxy trong Docker bridge network (vd: nginx container)
            // Mặc định ASP.NET chỉ trust 127.0.0.1 nên cần clear whitelist
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });
    }
}
