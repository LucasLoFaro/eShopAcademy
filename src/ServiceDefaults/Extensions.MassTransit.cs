using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using MassTransit;


namespace ServiceDefaults;

public static partial class Extentions
{
    public static TBuilder WithMassTransit<TBuilder>(
        this TBuilder builder,
        Action<IBusRegistrationContext, IBusFactoryConfigurator>? configureEndpoints = null,
        params Assembly[] assemblies) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddMassTransit(config =>
        {
            config.SetKebabCaseEndpointNameFormatter();
            config.SetInMemorySagaRepositoryProvider();
            config.AddSagaStateMachines(assemblies);
            config.AddActivities(assemblies);
            config.AddConsumers(assemblies);
            config.AddSagas(assemblies);

            if (builder.Environment.IsDevelopment())
            {
                config.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(new Uri(builder.Configuration.GetConnectionString("rabbit")!));
                    configureEndpoints?.Invoke(context, cfg); // Adding service-specific endpoints 
                    cfg.ConfigureEndpoints(context);
                });
            }
            else
            {
                config.UsingAzureServiceBus((context, cfg) =>
                {
                    cfg.Host(builder.Configuration.GetConnectionString("servicebus"));
                    configureEndpoints?.Invoke(context, cfg); // Adding service-specific endpoints 
                    cfg.ConfigureEndpoints(context);
                });
            }            
        });

        return builder;
    }
}
