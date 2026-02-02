extern alias OrdersApi;

using Core.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;

namespace Orders.Tests.Presentation.API;

/// <summary>
/// Custom WebApplicationFactory used to host the Order API during integration tests.
/// It replaces the real IOrderMessagingService with a mock implementation that
/// can be inspected and verified by tests.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<OrdersApi::Program>
{
    /// <summary>
    /// Gets the mock messaging service used during tests.  Tests can use this
    /// mock to verify that SubmitOrder was called with the expected request.
    /// </summary>
    public Mock<IOrderMessagingClient> MessagingServiceMock { get; } = new Mock<IOrderMessagingClient>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            // Remove the existing IOrderMessagingService registration if present
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IOrderMessagingClient));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }
            // Register the mock implementation
            services.AddSingleton<IOrderMessagingClient>(provider => MessagingServiceMock.Object);
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