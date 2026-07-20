using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace ServiceDefaults;

public static class HealthCheckTags
{
    public const string Live = "live";
    public const string Ready = "ready";
}

public static class HealthCheckDefaults
{
    public static readonly TimeSpan DependencyTimeout = TimeSpan.FromSeconds(1);
    public static readonly TimeSpan AggregateTimeout = TimeSpan.FromSeconds(2);
}

public static class HealthCheckExtensions
{
    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddSingleton<ShutdownReadinessHealthCheck>();
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), [HealthCheckTags.Live, HealthCheckTags.Ready])
            .AddCheck<ShutdownReadinessHealthCheck>("shutdown", tags: [HealthCheckTags.Ready]);
        return builder;
    }

    public static IHealthChecksBuilder AddCriticalDependency(
        this IHealthChecksBuilder builder,
        string name,
        Func<IServiceProvider, IHealthCheck> factory,
        TimeSpan? timeout = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(factory);

        builder.Add(new HealthCheckRegistration(
            name,
            factory,
            HealthStatus.Unhealthy,
            [HealthCheckTags.Ready],
            timeout ?? HealthCheckDefaults.DependencyTimeout));
        return builder;
    }

    public static IHealthChecksBuilder AddCriticalDependency(
        this IHealthChecksBuilder builder,
        string name,
        Func<IServiceProvider, CancellationToken, Task> check,
        TimeSpan? timeout = null) =>
        builder.AddCriticalDependency(name, sp => new DelegateHealthCheck(ct => check(sp, ct)), timeout);

    public static IEndpointConventionBuilder MapPlatformHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/alive", (HealthCheckService service, HttpContext context) =>
            SanitizedHealthEndpoint.WriteAsync(service, context, HealthCheckTags.Live));
        return endpoints.MapGet("/health", (HealthCheckService service, HttpContext context) =>
            SanitizedHealthEndpoint.WriteAsync(service, context, HealthCheckTags.Ready));
    }
}

internal sealed class DelegateHealthCheck(Func<CancellationToken, Task> check) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        await check(cancellationToken).ConfigureAwait(false);
        return HealthCheckResult.Healthy();
    }
}

internal sealed class ShutdownReadinessHealthCheck(IHostApplicationLifetime lifetime) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(lifetime.ApplicationStopping.IsCancellationRequested
            ? HealthCheckResult.Unhealthy()
            : HealthCheckResult.Healthy());
}

internal static class SanitizedHealthEndpoint
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task WriteAsync(
        HealthCheckService service,
        HttpContext context,
        string requiredTag)
    {
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
        budget.CancelAfter(HealthCheckDefaults.AggregateTimeout);

        HealthReport? report = null;
        try
        {
            report = await service.CheckHealthAsync(
                registration => registration.Tags.Contains(requiredTag),
                budget.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!context.RequestAborted.IsCancellationRequested)
        {
            // Aggregate timeout is represented as a fixed unhealthy response below.
        }

        var status = report?.Status ?? HealthStatus.Unhealthy;
        context.Response.StatusCode = status == HealthStatus.Healthy
            ? StatusCodes.Status200OK
            : StatusCodes.Status503ServiceUnavailable;
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = status.ToString(),
            checks = report?.Entries
                .OrderBy(entry => entry.Key, StringComparer.Ordinal)
                .Select(entry => new
                {
                    name = entry.Key,
                    status = entry.Value.Status.ToString(),
                    durationMs = Math.Round(entry.Value.Duration.TotalMilliseconds, 2)
                })
                .ToArray() ?? []
        };

        await JsonSerializer.SerializeAsync(context.Response.Body, response, JsonOptions, context.RequestAborted)
            .ConfigureAwait(false);
    }
}
