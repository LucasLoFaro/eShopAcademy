using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Orchestration.Data;
using Orchestration.Health;

namespace Orchestration.Tests.Health;

public sealed class OrderSagaDatabaseHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_UnreachableDatabase_ReturnsUnhealthy()
    {
        var services = new ServiceCollection();
        services.AddDbContext<OrderSagaDbContext>(options => options.UseNpgsql(
            "Host=127.0.0.1;Port=1;Database=missing;Username=missing;Password=missing;Timeout=1"));
        await using var provider = services.BuildServiceProvider();
        var healthCheck = new OrderSagaDatabaseHealthCheck(
            provider.GetRequiredService<IServiceScopeFactory>());

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.DoesNotContain("Password", result.Description, StringComparison.OrdinalIgnoreCase);
    }
}
