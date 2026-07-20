using System.Reflection;
using MassTransit;

namespace ServiceDefaults;

public sealed class MessagingHostConfigurator
{
    private readonly List<Assembly> _consumerAssemblies = [];
    private readonly List<EndpointDefinition> _endpoints = [];
    private readonly HashSet<string> _endpointNames = new(StringComparer.OrdinalIgnoreCase);

    internal IReadOnlyList<Assembly> ConsumerAssemblies => _consumerAssemblies;
    internal IReadOnlyList<EndpointDefinition> Endpoints => _endpoints;
    internal Action<IBusRegistrationConfigurator>? ConfigureRegistration { get; private set; }
    internal bool SchedulingEnabled { get; private set; }
    internal bool ReliabilityEnabled { get; private set; }
    internal MessagingReliabilityOptions? ReliabilityOptions { get; set; }

    public MessagingHostConfigurator AddConsumersFrom(params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies.Where(a => a is not null).Distinct())
        {
            if (!_consumerAssemblies.Contains(assembly))
            {
                _consumerAssemblies.Add(assembly);
            }
        }

        return this;
    }

    public MessagingHostConfigurator Registration(Action<IBusRegistrationConfigurator> configure)
    {
        ConfigureRegistration += configure ?? throw new ArgumentNullException(nameof(configure));
        return this;
    }

    public MessagingHostConfigurator UseScheduler()
    {
        SchedulingEnabled = true;
        return this;
    }

    public MessagingHostConfigurator UseReliabilityConventions()
    {
        ReliabilityEnabled = true;
        return this;
    }

    public MessagingHostConfigurator UseCorrelationId<TMessage>(Func<TMessage, Guid> provider)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(provider);
        GlobalTopology.Send.UseCorrelationId(provider);
        return this;
    }

    public MessagingHostConfigurator ReceiveEndpoint<TConsumer>(
        string endpointName,
        Action<IReceiveEndpointConfigurator>? configureEndpoint = null)
        where TConsumer : class, IConsumer
    {
        AddEndpoint(endpointName, (context, bus, name, createTopology) =>
        {
            bus.ReceiveEndpoint(name, endpoint =>
            {
                endpoint.ConfigureConsumeTopology = createTopology;
                configureEndpoint?.Invoke(endpoint);
                ApplyReliability(endpoint);
                endpoint.ConfigureConsumer<TConsumer>(context);
            });
        });

        return this;
    }

    public MessagingHostConfigurator ReceiveEndpoint<TConsumer>(
        string endpointName,
        Action<IBusRegistrationContext, IReceiveEndpointConfigurator> configureEndpoint)
        where TConsumer : class, IConsumer
    {
        ArgumentNullException.ThrowIfNull(configureEndpoint);
        AddEndpoint(endpointName, (context, bus, name, createTopology) =>
        {
            bus.ReceiveEndpoint(name, endpoint =>
            {
                endpoint.ConfigureConsumeTopology = createTopology;
                configureEndpoint(context, endpoint);
                ApplyReliability(endpoint);
                endpoint.ConfigureConsumer<TConsumer>(context);
            });
        });
        return this;
    }

    public MessagingHostConfigurator SagaEndpoint<TSaga>(
        string endpointName,
        Action<IBusRegistrationConfigurator> configurePersistence,
        Action<IReceiveEndpointConfigurator>? configureEndpoint = null)
        where TSaga : class, ISaga
    {
        Registration(configurePersistence ?? throw new ArgumentNullException(nameof(configurePersistence)));

        AddEndpoint(endpointName, (context, bus, name, createTopology) =>
        {
            bus.ReceiveEndpoint(name, endpoint =>
            {
                endpoint.ConfigureConsumeTopology = createTopology;
                configureEndpoint?.Invoke(endpoint);
                ApplyReliability(endpoint);
                endpoint.ConfigureSaga<TSaga>(context);
            });
        });

        return this;
    }

    private void ApplyReliability(IReceiveEndpointConfigurator endpoint)
    {
        if (ReliabilityEnabled)
        {
            MessagingReliabilityPolicy.Apply(
                endpoint,
                ReliabilityOptions ?? throw new InvalidOperationException("Messaging reliability options were not initialized."));
        }
    }

    private void AddEndpoint(string endpointName, EndpointConfiguration configure)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpointName);

        if (!_endpointNames.Add(endpointName))
        {
            throw new InvalidOperationException($"The MassTransit endpoint name '{endpointName}' is configured more than once in this host.");
        }

        _endpoints.Add(new EndpointDefinition(endpointName, configure));
    }
}

internal delegate void EndpointConfiguration(
    IBusRegistrationContext context,
    IBusFactoryConfigurator bus,
    string endpointName,
    bool createTopology);

internal sealed record EndpointDefinition(string Name, EndpointConfiguration Configure);
