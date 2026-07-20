using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace ServiceDefaults;

public sealed class HttpResilienceOptions
{
    public const string SectionName = "HttpResilience";

    public int MaxRetryAttempts { get; set; } = 2;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMilliseconds(200);
    public TimeSpan AttemptTimeout { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan TotalTimeout { get; set; } = TimeSpan.FromSeconds(15);
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(3);

    internal static HttpResilienceOptions Resolve(IConfiguration configuration)
    {
        var options = configuration.GetSection(SectionName).Get<HttpResilienceOptions>() ?? new();
        if (options.MaxRetryAttempts is < 0 or > 3 ||
            options.RetryDelay <= TimeSpan.Zero ||
            options.AttemptTimeout <= TimeSpan.Zero ||
            options.TotalTimeout <= options.AttemptTimeout ||
            options.ConnectTimeout <= TimeSpan.Zero ||
            options.ConnectTimeout > options.AttemptTimeout)
        {
            throw new InvalidOperationException(
                "HttpResilience configuration is invalid; retry attempts must be 0-3 and connect/attempt/total timeout budgets must be positive and ordered.");
        }

        return options;
    }
}

public static class HttpResilienceExtensions
{
    public static IHttpClientBuilder AddSafeHttpResilience(
        this IHttpClientBuilder builder,
        HttpResilienceOptions? options = null)
    {
        options ??= new HttpResilienceOptions();
        ConfigureHandler(builder, options);
        return builder;
    }

    public static IHttpClientBuilder AddIdempotentHttpResilience(this IHttpClientBuilder builder)
    {
        builder.Services.AddTransient<RequireIdempotencyKeyHandler>();
        builder.AddHttpMessageHandler<RequireIdempotencyKeyHandler>();
        return builder;
    }

    private static void ConfigureHandler(IHttpClientBuilder builder, HttpResilienceOptions options)
    {
        builder.ConfigureHttpClient(client => client.Timeout = options.TotalTimeout)
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                ConnectTimeout = options.ConnectTimeout,
                PooledConnectionLifetime = TimeSpan.FromMinutes(5)
            })
            .AddResilienceHandler("safe-default", pipeline =>
            {
                var retry = new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = options.MaxRetryAttempts,
                    Delay = options.RetryDelay,
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    ShouldRetryAfterHeader = true,
                    ShouldHandle = arguments =>
                    {
                        var request = arguments.Context.GetRequestMessage() ?? arguments.Outcome.Result?.RequestMessage;
                        if (request is not null && IsUnsafe(request.Method) &&
                            !request.Headers.Contains("Idempotency-Key"))
                        {
                            return ValueTask.FromResult(false);
                        }

                        return ValueTask.FromResult(HttpClientResiliencePredicates.IsTransient(arguments.Outcome));
                    }
                };

                pipeline.AddTimeout(options.TotalTimeout);
                pipeline.AddRetry(retry);
                pipeline.AddTimeout(options.AttemptTimeout);
            });
    }

    internal static bool IsUnsafe(HttpMethod method) =>
        method == HttpMethod.Post || method == HttpMethod.Patch || method == HttpMethod.Delete ||
        method == HttpMethod.Put || method.Method.Equals("CONNECT", StringComparison.OrdinalIgnoreCase);
}

internal sealed class RequireIdempotencyKeyHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (HttpResilienceExtensions.IsUnsafe(request.Method) && !request.Headers.Contains("Idempotency-Key"))
        {
            throw new InvalidOperationException(
                "Unsafe HTTP retry requires an Idempotency-Key header backed by a durable idempotency strategy.");
        }

        return base.SendAsync(request, cancellationToken);
    }
}
