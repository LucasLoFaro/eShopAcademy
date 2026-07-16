using System.Reflection;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ServiceDefaults;

public static partial class Extentions
{
    public static TBuilder WithMassTransit<TBuilder>(
        this TBuilder builder,
        Action<MessagingHostConfigurator>? configure = null,
        params Assembly[] assemblies)
        where TBuilder : IHostApplicationBuilder
    {
        var hostConfiguration = new MessagingHostConfigurator();
        hostConfiguration.AddConsumersFrom(assemblies);
        configure?.Invoke(hostConfiguration);

        var messagingOptions = MessagingOptionsResolver.Resolve(
            builder.Configuration,
            builder.Environment);

        builder.Services.AddSingleton(messagingOptions);
        builder.Services.AddSingleton<IOptions<MessagingOptions>>(Options.Create(messagingOptions));
        builder.Services.AddSingleton(new MessagingRegistrationMetadata(
            messagingOptions.Transport,
            hostConfiguration.SchedulingEnabled
                ? messagingOptions.Transport == MessagingTransport.RabbitMq
                    ? MessagingScheduler.Quartz
                    : MessagingScheduler.AzureServiceBusNative
                : MessagingScheduler.None,
            messagingOptions.Transport == MessagingTransport.RabbitMq ||
            messagingOptions.AzureServiceBus.CreateTopology));
        builder.Services.AddMassTransit(registration =>
        {
            registration.SetKebabCaseEndpointNameFormatter();

            foreach (var assembly in hostConfiguration.ConsumerAssemblies)
            {
                registration.AddConsumers(assembly);
            }

            hostConfiguration.ConfigureRegistration?.Invoke(registration);

            if (hostConfiguration.SchedulingEnabled &&
                messagingOptions.Transport == MessagingTransport.RabbitMq)
            {
                registration.AddQuartzConsumers();
            }

            if (messagingOptions.Transport == MessagingTransport.RabbitMq)
            {
                RabbitMqTransportConfigurator.Configure(
                    registration,
                    builder.Services,
                    messagingOptions,
                    hostConfiguration);
            }
            else
            {
                AzureServiceBusTransportConfigurator.Configure(
                    registration,
                    messagingOptions,
                    hostConfiguration);
            }
        });

        return builder;
    }
}
