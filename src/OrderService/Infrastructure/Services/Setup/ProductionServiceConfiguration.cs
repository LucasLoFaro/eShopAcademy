using Microsoft.Extensions.DependencyInjection;
using Infrastructure.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using MassTransit;


namespace Infrastructure.Services.Setup;

public static class ProductionServiceConfiguration
{
    public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddMassTransit(x =>
        {
            x.SetKebabCaseEndpointNameFormatter();
            x.UsingAzureServiceBus((context, cfg) =>
            {
                cfg.Host(configuration["ServiceBus:Host"]);
                cfg.ConfigureEndpoints(context);
            });
        });

        services.AddSingleton<IOrderMessagingService, AzureServiceBusPublisher>();
        services.AddSingleton<IAppConfigurationService, AzureAppConfigurationService>();
    }
}
