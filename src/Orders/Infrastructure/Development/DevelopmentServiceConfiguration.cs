using Core.Domain.Contracts;
using Infrastructure.Services.Interfaces;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

namespace Infrastructure.Development;

/// <summary>
/// Registers in-memory and stub implementations for local development.  When running
/// the order service locally, we do not want to connect to Azure Service Bus or
/// Azure App Configuration.  This configuration class sets up MassTransit to
/// use the in‑memory transport and provides a simple stub messaging service.
/// </summary>
public static class DevelopmentServiceConfiguration
{
    public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Use MassTransit's in-memory transport so no external broker is required.
        services.AddMassTransit(x =>
        {
            x.UsingInMemory((context, cfg) =>
            {
                cfg.ConfigureEndpoints(context);
            });
        });

        // Register a stubbed messaging service that captures submitted orders but does
        // not publish them to an actual message broker.
        services.AddSingleton<IOrderMessagingService, StubOrderMessagingService>();
    }
}

/// <summary>
/// A stub implementation of <see cref="IOrderMessagingService"/> used during
/// development.  It records the most recent order request and returns
/// immediately without contacting any external services.
/// </summary>
internal class StubOrderMessagingService : IOrderMessagingService
{
    public OrderRequest? LastOrder { get; private set; }

    public Task SubmitOrder(OrderRequest orderRequest)
    {
        LastOrder = orderRequest;
        return Task.CompletedTask;
    }
}