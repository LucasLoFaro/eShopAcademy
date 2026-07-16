using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Quartz;
using ServiceDefaults;
using System.Text.Json;

namespace ServiceDefaults.Tests;

public class MessagingRegistrationTests
{
    [Theory]
    [InlineData(MessagingTransport.RabbitMq, false, true, MessagingScheduler.Quartz)]
    [InlineData(MessagingTransport.AzureServiceBus, false, false, MessagingScheduler.AzureServiceBusNative)]
    [InlineData(MessagingTransport.AzureServiceBus, true, true, MessagingScheduler.AzureServiceBusNative)]
    public void Registration_builds_without_connecting_to_broker(
        MessagingTransport transport,
        bool createTopology,
        bool expectedConsumeTopology,
        MessagingScheduler expectedScheduler)
    {
        var builder = CreateBuilder(transport, createTopology: createTopology);
        var endpointCallbackInvoked = false;
        var sagaEndpointCallbackInvoked = false;
        var registrationCallbackCount = 0;
        builder.WithMassTransit(messaging =>
        {
            messaging.AddConsumersFrom(typeof(TestConsumer).Assembly);
            messaging.AddConsumersFrom(typeof(TestConsumer).Assembly);
            messaging.Registration(registration =>
            {
                registrationCallbackCount++;
            });
            messaging.Registration(_ => registrationCallbackCount++);
            messaging.ReceiveEndpoint<TestConsumer>("test-consumer", endpoint =>
            {
                endpoint.ConcurrentMessageLimit = 1;
                endpointCallbackInvoked = true;
            });
            messaging.SagaEndpoint<TestSaga>(
                "test-saga",
                registration => registration.AddSaga<TestSaga>().InMemoryRepository(),
                _ => sagaEndpointCallbackInvoked = true);
            messaging.UseScheduler();
        });

        using var host = builder.Build();

        var bus = host.Services.GetRequiredService<IBusControl>();
        var metadata = host.Services.GetRequiredService<MessagingRegistrationMetadata>();
        var endpointNameFormatter = host.Services.GetRequiredService<IEndpointNameFormatter>();
        var probe = JsonSerializer.Serialize(bus.GetProbeResult().Results);

        Assert.True(endpointCallbackInvoked);
        Assert.True(sagaEndpointCallbackInvoked);
        Assert.Equal(2, registrationCallbackCount);
        Assert.Equal(transport, metadata.Transport);
        Assert.Equal(expectedScheduler, metadata.Scheduler);
        Assert.Equal(expectedConsumeTopology, metadata.RuntimeTopologyCreation);
        Assert.Equal("test", endpointNameFormatter.Consumer<TestConsumer>());
        Assert.Contains("test-consumer", probe, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("test-saga", probe, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("scheduler", probe, StringComparison.OrdinalIgnoreCase);

        if (transport == MessagingTransport.RabbitMq)
        {
            Assert.NotNull(host.Services.GetService<ISchedulerFactory>());
            Assert.Contains("quartz", probe, StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            Assert.Null(host.Services.GetService<ISchedulerFactory>());
            Assert.Contains("servicebus", probe, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Theory]
    [InlineData(MessagingTransport.RabbitMq)]
    [InlineData(MessagingTransport.AzureServiceBus)]
    public void Scheduling_is_opt_in_for_both_transports(MessagingTransport transport)
    {
        var builder = CreateBuilder(transport);
        builder.WithMassTransit();

        using var host = builder.Build();
        var metadata = host.Services.GetRequiredService<MessagingRegistrationMetadata>();

        Assert.Equal(MessagingScheduler.None, metadata.Scheduler);
        Assert.Null(host.Services.GetService<ISchedulerFactory>());
    }

    [Fact]
    public void Sagas_require_explicit_registration_and_persistence()
    {
        var messaging = new MessagingHostConfigurator();

        Assert.Throws<ArgumentNullException>(() =>
            messaging.SagaEndpoint<TestSaga>("test-saga", configurePersistence: null!));
    }

    [Fact]
    public void Explicit_transport_overrides_environment_default()
    {
        var builder = CreateBuilder(MessagingTransport.AzureServiceBus, Environments.Development);
        builder.WithMassTransit();

        using var host = builder.Build();

        Assert.Equal(
            MessagingTransport.AzureServiceBus,
            host.Services.GetRequiredService<IOptions<MessagingOptions>>().Value.Transport);
    }

    [Fact]
    public void Conflicting_endpoint_names_are_rejected()
    {
        var messaging = new MessagingHostConfigurator();
        messaging.ReceiveEndpoint<TestConsumer>("conflict");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            messaging.ReceiveEndpoint<TestConsumer>("CONFLICT"));

        Assert.Contains("configured more than once", exception.Message);
    }

    [Fact]
    public void Invalid_azure_namespace_is_rejected_during_registration()
    {
        var builder = new HostApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = Environments.Production
        });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Messaging:Transport"] = "AzureServiceBus",
            ["Messaging:AzureServiceBus:NamespaceUri"] = "not-a-uri"
        });

        Assert.Throws<Microsoft.Extensions.Options.OptionsValidationException>(() =>
            builder.WithMassTransit());
    }

    private static HostApplicationBuilder CreateBuilder(
        MessagingTransport transport,
        string environment = "Testing",
        bool createTopology = false)
    {
        var builder = new HostApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = environment
        });

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Messaging:Transport"] = transport.ToString(),
            ["Messaging:RabbitMq:ConnectionString"] = "amqp://guest:guest@localhost:5672",
            ["Messaging:AzureServiceBus:NamespaceUri"] = "https://example.servicebus.windows.net",
            ["Messaging:AzureServiceBus:CreateTopology"] = createTopology.ToString()
        });

        return builder;
    }

    public sealed class TestConsumer : IConsumer<TestMessage>
    {
        public Task Consume(ConsumeContext<TestMessage> context) => Task.CompletedTask;
    }

    public sealed record TestMessage;

    public sealed class TestSaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }
}
