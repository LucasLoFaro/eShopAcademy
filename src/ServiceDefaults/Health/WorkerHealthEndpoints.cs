using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ServiceDefaults;

public sealed class WorkerHealthEndpointOptions
{
    public const string SectionName = "Management:Health";
    public string Url { get; set; } = "http://0.0.0.0:8081";
}

public static class WorkerHealthEndpointExtensions
{
    public static TBuilder AddWorkerHealthEndpoints<TBuilder>(
        this TBuilder builder,
        Action<WorkerHealthEndpointOptions>? configure = null)
        where TBuilder : IHostApplicationBuilder
    {
        var options = builder.Services.AddOptions<WorkerHealthEndpointOptions>()
            .Bind(builder.Configuration.GetSection(WorkerHealthEndpointOptions.SectionName))
            .Validate(
                value => Uri.TryCreate(value.Url, UriKind.Absolute, out var uri) &&
                         (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps),
                "Management:Health:Url must be an absolute HTTP or HTTPS URL.")
            .ValidateOnStart();
        if (configure is not null)
        {
            options.Configure(configure);
        }

        builder.Services.AddHostedService<WorkerHealthEndpointService>();
        return builder;
    }
}

internal sealed class WorkerHealthEndpointService(
    IOptions<WorkerHealthEndpointOptions> options,
    HealthCheckService healthChecks,
    ILogger<WorkerHealthEndpointService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls(options.Value.Url);
        var app = builder.Build();
        app.MapGet("/alive", context => SanitizedHealthEndpoint.WriteAsync(healthChecks, context, HealthCheckTags.Live));
        app.MapGet("/health", context => SanitizedHealthEndpoint.WriteAsync(healthChecks, context, HealthCheckTags.Ready));

        await app.StartAsync(stoppingToken).ConfigureAwait(false);
        logger.LogInformation("Worker management health endpoint started");
        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        finally
        {
            await app.StopAsync(CancellationToken.None).ConfigureAwait(false);
            await app.DisposeAsync().ConfigureAwait(false);
        }
    }
}
