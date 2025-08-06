using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Core.Domain.Contracts;
using Infrastructure.Services.Interfaces;

namespace Tests;

/// <summary>
/// Custom WebApplicationFactory used to host the Order API during integration tests.
/// It replaces the real IOrderMessagingService with a stub implementation that
/// captures submitted orders for verification.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    /// <summary>
    /// Gets the stub messaging service used during tests.  This allows tests
    /// to inspect the orders submitted by the API.
    /// </summary>
    public StubOrderMessagingService StubMessaging { get; } = new StubOrderMessagingService();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing IOrderMessagingService registration if present
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IOrderMessagingService));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }
            // Register the stub implementation
            services.AddSingleton<IOrderMessagingService>(StubMessaging);
        });
    }
}

/// <summary>
/// Stub implementation of IOrderMessagingService for use in integration tests.
/// It stores the last order request submitted so that tests can verify the
/// API published the expected command.
/// </summary>
public class StubOrderMessagingService : IOrderMessagingService
{
    /// <summary>
    /// Gets the last order request submitted via this messaging service.
    /// </summary>
    public OrderRequest? LastRequest { get; private set; }

    public Task SubmitOrder(OrderRequest orderRequest)
    {
        LastRequest = orderRequest;
        return Task.CompletedTask;
    }
}