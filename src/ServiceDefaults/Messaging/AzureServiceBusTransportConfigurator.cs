using Azure.Identity;
using MassTransit;

namespace ServiceDefaults;

internal static class AzureServiceBusTransportConfigurator
{
    public static void Configure(
        IBusRegistrationConfigurator registration,
        MessagingOptions options,
        MessagingHostConfigurator host)
    {
        if (!options.AzureServiceBus.CreateTopology)
        {
            registration.AddConfigureEndpointsCallback((_, endpoint) =>
                endpoint.ConfigureConsumeTopology = false);
        }

        registration.UsingAzureServiceBus((context, bus) =>
        {
            var configuredNamespace = new Uri(options.AzureServiceBus.NamespaceUri!);
            var serviceBusNamespace = configuredNamespace.Scheme == "sb"
                ? configuredNamespace
                : new UriBuilder(configuredNamespace)
                {
                    Scheme = "sb",
                    Port = -1
                }.Uri;

            bus.Host(serviceBusNamespace, hostConfiguration =>
            {
                hostConfiguration.TokenCredential = new DefaultAzureCredential();
            });

            bus.DeployPublishTopology = options.AzureServiceBus.CreateTopology;

            if (host.SchedulingEnabled)
            {
                bus.UseServiceBusMessageScheduler();
            }

            RabbitMqTransportConfigurator.ConfigureExplicitEndpoints(
                context,
                bus,
                host,
                options.AzureServiceBus.CreateTopology);
            bus.ConfigureEndpoints(context);
        });
    }
}
