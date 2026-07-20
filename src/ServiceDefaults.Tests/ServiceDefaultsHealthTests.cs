using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace ServiceDefaults.Tests;

public class ServiceDefaultsHealthTests
{
    [Fact]
    public async Task Health_registration_is_available_when_monitoring_is_disabled()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration["Aspire:Monitoring:Enabled"] = "false";
        builder.AddServiceDefaults();

        using var host = builder.Build();
        var healthChecks = host.Services.GetRequiredService<HealthCheckService>();

        var result = await healthChecks.CheckHealthAsync(check => check.Name == "self");

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Contains("self", result.Entries.Keys);
    }
}
