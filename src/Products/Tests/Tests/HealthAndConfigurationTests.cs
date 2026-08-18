using System.Net;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace Tests;

[TestFixture]
public class HealthAndConfigurationTests
{
    [Test]
    public async Task DependencyFailure_FailsReadinessButNotLiveness()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseTestServer();
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"])
            .AddCheck("products-mongodb", () => HealthCheckResult.Unhealthy(), ["ready"]);
        await using var app = builder.Build();
        app.UseDefaultEndpoints();
        await app.StartAsync();

        Assert.That((await app.GetTestClient().GetAsync("/health")).StatusCode,
            Is.EqualTo(HttpStatusCode.ServiceUnavailable));
        Assert.That((await app.GetTestClient().GetAsync("/alive")).StatusCode,
            Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public void MissingProductDatabase_FailsOptionsValidation()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddProductStorage(new ConfigurationBuilder().Build());
        using var provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() =>
            _ = provider.GetRequiredService<IOptions<ProductStorageOptions>>().Value);
    }

    [Test]
    public void NonHttpsContentSafetyEndpoint_FailsOptionsValidation()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ContentSafety:Endpoint"] = "http://unsafe" })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddProductContentModeration(configuration, new Microsoft.Extensions.Hosting.Internal.HostingEnvironment { EnvironmentName = "Development" });
        using var provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() =>
            _ = provider.GetRequiredService<IOptions<ContentSafetyOptions>>().Value);
    }
}
