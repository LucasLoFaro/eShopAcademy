using Azure.Identity;
using EventsProcessor.StateMachines;
using Infrastructure.Services.Settings;
using MassTransit;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;

var builder = Host.CreateApplicationBuilder(args);

// Acquire a credential that can access both App Configuration and Key Vault.
var credential = new DefaultAzureCredential();

// Load configuration from Azure App Configuration.  This allows the service bus
// connection information to be centrally managed.  Keys are prefaced with
// "common:ServiceBusSettings" and "order:" scopes.
builder.Configuration.AddAzureAppConfiguration(options =>
    options.Connect(
        new Uri($"https://{Environment.GetEnvironmentVariable("APPCONFIGURATION")}.azconfig.io"),
        credential)
    .ConfigureKeyVault(kv => { kv.SetCredential(credential); })
    .Select("common:*", LabelFilter.Null)
    .Select("order:*", LabelFilter.Null)
    );

var sbSettings = builder.Configuration.GetSection("common:ServiceBusSettings").Get<ServiceBusSettings>();

// Register MassTransit and the saga state machine.  We use the in-memory saga
// repository for simplicity.  In a production scenario you would likely
// configure a durable provider such as Entity Framework or Cosmos DB.
builder.Services.AddMassTransit(x =>
{
    // Use kebab-case naming for endpoints so queues follow a consistent convention.
    x.SetKebabCaseEndpointNameFormatter();

    // Register the state machine and its state.  The InMemoryRepository is
    // sufficient for demos but should be replaced with a persistent store.
    x.AddSagaStateMachine<OrderStateMachine, OrderState>()
        .InMemoryRepository();

    x.UsingAzureServiceBus((context, cfg) =>
    {
        cfg.Host($"sb://{sbSettings!.Host}/", h =>
        {
            h.TokenCredential = credential;
        });

        // Configure the saga endpoint.  MassTransit will automatically
        // subscribe the SubmitOrder event to this endpoint and forward it
        // to the state machine.
        cfg.ReceiveEndpoint("submit-order", e =>
        {
            e.ConfigureSaga<OrderState>(context);
        });

        cfg.ConfigureEndpoints(context);
    });
});

var host = builder.Build();
await host.RunAsync();