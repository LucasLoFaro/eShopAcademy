using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ServiceDiscovery;
using Microsoft.AspNetCore.Builder;
using ServiceDefaults;


namespace Microsoft.Extensions.Hosting;

public static partial class Extensions
{
    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        var enableMonitoring = builder.Configuration.GetValue("Aspire:Monitoring:Enabled", true);

        builder.AddDefaultConfiguration();
        if (enableMonitoring)
        {
            builder.ConfigureOpenTelemetry();
            builder.AddDefaultHealthChecks();
        }
        builder.Services.AddServiceDiscovery();
        builder.Services.Configure<ServiceDiscoveryOptions>(options =>
        {
            options.AllowedSchemes = ["http"];
        });
        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });
        
        builder.Services.AddAutoMapper(cfg => { }, AppDomain.CurrentDomain.GetAssemblies());

        return builder;
    }


    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    public static WebApplication UseDefaultEndpoints(this WebApplication app)
    {
        //app.MapDefaultEndpoints();

        //app.UseHttpsRedirection(); // This adds a second trace id for the request redirect while testing locally. Shouldn't happen in prod if I use https only. 
        app.UseRouting();

        if (app.Services.GetService<HealthCheckService>() is not null)
        {
            app.MapHealthChecks("/health");
            app.MapHealthChecks("/alive", new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("live")
            });
        }
        app.UseSwaggerIfEnabled();

        return app;
    }
}
