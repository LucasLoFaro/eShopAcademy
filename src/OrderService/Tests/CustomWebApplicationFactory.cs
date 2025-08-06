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

    /// <summary>
    /// Override CreateHost to set required environment variables before the
    /// application is built.  Without these values, the AddAzureAppConfiguration
    /// call in Program.cs attempts to construct a Uri from an empty
    /// APPCONFIGURATION variable, leading to an Invalid URI exception.
    /// </summary>
    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Ensure APPCONFIGURATION has a dummy value to produce a valid URI.  The actual
        // connection to Azure App Configuration is not used during tests.
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPCONFIGURATION")))
        {
            Environment.SetEnvironmentVariable("APPCONFIGURATION", "test-config");
        }
        // Similarly, set a default host for ServiceBusSettings to avoid null reference
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SERVICEBUS_HOST")))
        {
            Environment.SetEnvironmentVariable("SERVICEBUS_HOST", "localhost");
        }
        return base.CreateHost(builder);
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