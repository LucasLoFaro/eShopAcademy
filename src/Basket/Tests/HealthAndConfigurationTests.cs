using System.Net;
using Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Basket.Tests;

public class HealthAndConfigurationTests
{
    [Fact]
    public async Task DependencyFailure_FailsReadinessButKeepsLivenessHealthy()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseTestServer();
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"])
            .AddCheck("critical", () => HealthCheckResult.Unhealthy("secret connection detail"), ["ready"]);

        await using var app = builder.Build();
        app.UseDefaultEndpoints();
        await app.StartAsync();

        var client = app.GetTestClient();
        var readiness = await client.GetAsync("/health");
        var liveness = await client.GetAsync("/alive");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, readiness.StatusCode);
        Assert.Equal(HttpStatusCode.OK, liveness.StatusCode);
        Assert.DoesNotContain("secret connection detail", await readiness.Content.ReadAsStringAsync());
    }

    [Fact]
    public void MissingRedisConnection_FailsOptionsValidation()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBasketStorage(new ConfigurationBuilder().Build());

        using var provider = services.BuildServiceProvider();
        Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<BasketRedisOptions>>().Value);
    }
}
