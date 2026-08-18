using Data.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace Data;

public sealed class BasketRedisOptions
{
    public string ConnectionString { get; set; } = string.Empty;
}

public static class BasketStorage
{
    public static IServiceCollection AddBasketStorage(
        this IServiceCollection services,
        IConfiguration configuration,
        bool includeProductCache = false)
    {
        services.AddOptions<BasketRedisOptions>()
            .Configure(options => options.ConnectionString = configuration.GetConnectionString("Redis") ?? string.Empty)
            .Validate(options => !string.IsNullOrWhiteSpace(options.ConnectionString),
                "ConnectionStrings:Redis is required.")
            .ValidateOnStart();

        services.AddSingleton<IDatabaseClient, DatabaseClient>();
        services.AddTransient<IBasketCache, BasketCache>();
        if (includeProductCache)
        {
            services.AddTransient<IProductCache, ProductCache>();
        }

        services.AddHealthChecks().AddCheck<RedisReadinessHealthCheck>(
            "redis",
            failureStatus: HealthStatus.Unhealthy,
            tags: ["ready"],
            timeout: TimeSpan.FromSeconds(3));

        return services;
    }
}

public sealed class RedisReadinessHealthCheck : IHealthCheck
{
    private readonly IDatabaseClient _database;

    public RedisReadinessHealthCheck(IDatabaseClient database) => _database = database;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _database.PingAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception exception) when (exception is RedisException or TimeoutException or OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("Redis is unavailable.");
        }
    }
}
