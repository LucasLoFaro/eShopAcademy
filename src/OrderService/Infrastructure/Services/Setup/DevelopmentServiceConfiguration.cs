using Microsoft.Extensions.DependencyInjection;
using Infrastructure.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using MassTransit;


namespace Infrastructure.Services.Setup;

public static class DevelopmentServiceConfiguration
{
    public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Configure MassTransit to use the in-memory transport for local development.
        services.AddMassTransit(x =>
        {
            x.UsingInMemory((context, cfg) =>
            {
                cfg.ConfigureEndpoints(context);
            });
        });

        // Register a messaging service that publishes commands via MassTransit.  When
        // using the in-memory transport this will send messages to the local bus.
        services.AddScoped<IOrderMessagingService, InMemoryOrderMessagingService>();
    }
}
