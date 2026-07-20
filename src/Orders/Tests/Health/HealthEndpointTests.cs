using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Orders.Tests.Health;

public sealed class HealthEndpointTests
{
    [Fact]
    public async Task Alive_IsSelfOnly_WhenReadinessDependencyFails()
    {
        await using var app = await StartApp(includeFailedDependency: true);
        var client = app.GetTestClient();

        var alive = await client.GetAsync("/alive");
        var health = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, alive.StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, health.StatusCode);
        Assert.DoesNotContain("connection-string-secret", await health.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Health_IsHealthy_WhenCriticalDependenciesAreHealthy()
    {
        await using var app = await StartApp(includeFailedDependency: false);
        var response = await app.GetTestClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task<WebApplication> StartApp(bool includeFailedDependency)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseTestServer();
        builder.Configuration["Aspire:Monitoring:Enabled"] = "false";
        builder.AddServiceDefaults();

        if (includeFailedDependency)
        {
            builder.Services.AddHealthChecks().AddCheck(
                "broker",
                () => HealthCheckResult.Unhealthy("connection-string-secret"),
                tags: ["ready"]);
        }

        var app = builder.Build();
        app.UseDefaultEndpoints();
        await app.StartAsync();
        return app;
    }
}
