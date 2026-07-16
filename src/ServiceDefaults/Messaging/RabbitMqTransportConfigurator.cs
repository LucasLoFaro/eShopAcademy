using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace ServiceDefaults;

internal static class RabbitMqTransportConfigurator
{
    internal const string QuartzEndpointName = "quartz";

    public static void Configure(
        IBusRegistrationConfigurator registration,
        IServiceCollection services,
        MessagingOptions options,
        MessagingHostConfigurator host)
    {
        registration.UsingRabbitMq((context, bus) =>
        {
            bus.Host(new Uri(options.RabbitMq.ConnectionString!));

            if (host.SchedulingEnabled)
            {
                bus.UseMessageScheduler(new Uri($"queue:{QuartzEndpointName}"));
            }

            ConfigureExplicitEndpoints(context, bus, host, createTopology: true);
            bus.ConfigureEndpoints(context);
        });

        if (host.SchedulingEnabled)
        {
            services.AddQuartz();
            services.AddQuartzHostedService(configuration =>
                configuration.WaitForJobsToComplete = true);
        }
    }

    internal static void ConfigureExplicitEndpoints(
        IBusRegistrationContext context,
        IBusFactoryConfigurator bus,
        MessagingHostConfigurator host,
        bool createTopology)
    {
        foreach (var endpoint in host.Endpoints)
        {
            endpoint.Configure(context, bus, endpoint.Name, createTopology);
        }

    }
}
