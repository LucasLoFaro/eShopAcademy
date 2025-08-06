using Microsoft.Extensions.DependencyInjection;
using Infrastructure.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using MassTransit;


namespace Infrastructure.Services.Setup;

public static class DevelopmentServiceConfiguration
{
    public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddMassTransit(x =>
        {
            x.UsingInMemory((context, cfg) =>
            {
                cfg.ConfigureEndpoints(context);
            });
        });

        services.AddSingleton<IOrderMessagingService, StubOrderMessagingService>();
        services.AddSingleton<IAppConfigurationService, StubAppConfigurationService>();
    }
}
