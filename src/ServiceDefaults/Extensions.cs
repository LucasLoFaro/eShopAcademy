using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.ServiceDiscovery;
using ServiceDefaults;

namespace Microsoft.Extensions.Hosting;

public static partial class Extensions
{
    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.AddDefaultConfiguration();
        builder.AddDefaultHealthChecks();

        if (builder.Configuration.GetValue("Aspire:Monitoring:Enabled", true))
        {
            builder.ConfigureOpenTelemetry();
        }

        builder.Services.AddServiceDiscovery();
        builder.Services.Configure<ServiceDiscoveryOptions>(options => options.AllowedSchemes = ["http", "https"]);
        var httpResilience = HttpResilienceOptions.Resolve(builder.Configuration);
        builder.Services.AddSingleton(httpResilience);
        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddSafeHttpResilience(httpResilience);
            http.AddServiceDiscovery();
        });

        builder.Services.AddAutoMapper(_ => { }, AppDomain.CurrentDomain.GetAssemblies());
        return builder;
    }

    public static WebApplication UseDefaultEndpoints(this WebApplication app)
    {
        app.MapPlatformHealthEndpoints();
        app.UseSwaggerIfEnabled();
        return app;
    }
}
