using Microsoft.Extensions.DependencyInjection;
using Infrastructure.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using MassTransit;


namespace Infrastructure.Services.Setup;

public static class ProductionServiceConfiguration
{
    public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Configure MassTransit to use Azure Service Bus in production.  The
        // namespace host is configured via ServiceBusSettings.
        services.AddMassTransit(x =>
        {
            x.SetKebabCaseEndpointNameFormatter();
            x.UsingAzureServiceBus((context, cfg) =>
            {
                cfg.Host(configuration["ServiceBus:Host"]);
                cfg.ConfigureEndpoints(context);
            });
        });

        // Register the messaging service implementation that publishes commands to
        // Azure Service Bus.
        services.AddSingleton<IOrderMessagingService, OrderMessageClient>();
    }
}
