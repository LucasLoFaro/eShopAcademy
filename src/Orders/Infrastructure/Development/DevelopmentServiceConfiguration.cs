using Core.Domain.Contracts;
using Infrastructure.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

namespace Infrastructure.Development;

/// <summary>
/// Registers stub implementations for isolated order-service development.
/// </summary>
public static class DevelopmentServiceConfiguration
{
    public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
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
