using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ServiceDefaults;

namespace ServiceDefaults.Tests;

public class ServiceDefaultsHealthTests
{
    [Fact]
    public async Task Alive_is_self_only_while_health_checks_critical_dependencies()
    {
        var calls = 0;
        await using var app = await CreateApp(services =>
            services.AddHealthChecks().AddCriticalDependency(
                "critical-store",
                (_, _) =>
                {
                    calls++;
                    throw new InvalidOperationException("database unavailable");
                }));
        var client = app.GetTestClient();

        var alive = await client.GetAsync("/alive");
        Assert.Equal(HttpStatusCode.OK, alive.StatusCode);
        Assert.Equal(0, calls);

        var health = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, health.StatusCode);
        Assert.Equal(1, calls);
        Assert.Contains("critical-store", await health.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Health_response_never_exposes_sensitive_failure_details()
    {
        const string secret = "AccountKey=health-secret;HostName=private-db.internal";
        await using var app = await CreateApp(services =>
            services.AddHealthChecks().AddCriticalDependency(
                "orders-store",
                (_, _) => throw new InvalidOperationException(secret)));

        var response = await app.GetTestClient().GetAsync("/health");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Contains("orders-store", body);
        Assert.DoesNotContain("AccountKey", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("health-secret", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("private-db", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("exception", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Telemetry_disabled_does_not_disable_health_endpoints()
    {
        await using var app = await CreateApp();
        var client = app.GetTestClient();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/alive")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
    }

    private static async Task<WebApplication> CreateApp(Action<IServiceCollection>? configure = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Testing"
        });
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Aspire:Monitoring:Enabled"] = "false"
        });
        builder.AddWebServiceDefaults();
        configure?.Invoke(builder.Services);

        var app = builder.Build();
        app.UseWebServiceDefaults();
        app.UseDefaultEndpoints();
        await app.StartAsync();
        return app;
    }
}
