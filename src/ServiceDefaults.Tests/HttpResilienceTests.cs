using System.Net;
using Microsoft.Extensions.DependencyInjection;
using ServiceDefaults;

namespace ServiceDefaults.Tests;

public class HttpResilienceTests
{
    [Theory]
    [InlineData("POST")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task Unsafe_methods_are_not_automatically_retried(string method)
    {
        var handler = new CountingHandler();
        var services = new ServiceCollection();
        services.AddHttpClient("unsafe")
            .AddSafeHttpResilience(new HttpResilienceOptions
            {
                MaxRetryAttempts = 2,
                RetryDelay = TimeSpan.FromMilliseconds(1),
                AttemptTimeout = TimeSpan.FromSeconds(1),
                TotalTimeout = TimeSpan.FromSeconds(3),
                ConnectTimeout = TimeSpan.FromMilliseconds(100)
            })
            .ConfigurePrimaryHttpMessageHandler(() => handler);
        await using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("unsafe");

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), "https://example.invalid/payment"));

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(1, handler.Attempts);
    }

    [Fact]
    public async Task Idempotent_unsafe_policy_rejects_missing_idempotency_key_before_send()
    {
        var handler = new CountingHandler();
        var services = new ServiceCollection();
        services.AddHttpClient("idempotent")
            .AddIdempotentHttpResilience()
            .ConfigurePrimaryHttpMessageHandler(() => handler);
        await using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("idempotent");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.PostAsync("https://example.invalid/refund", new StringContent("{}")));

        Assert.Contains("Idempotency-Key", exception.Message);
        Assert.Equal(0, handler.Attempts);
    }

    [Fact]
    public async Task Unsafe_retry_is_an_explicit_idempotency_key_opt_in()
    {
        var handler = new CountingHandler();
        var services = new ServiceCollection();
        services.AddHttpClient("idempotent")
            .AddSafeHttpResilience(new HttpResilienceOptions
            {
                MaxRetryAttempts = 2,
                RetryDelay = TimeSpan.FromMilliseconds(1),
                AttemptTimeout = TimeSpan.FromSeconds(1),
                TotalTimeout = TimeSpan.FromSeconds(3),
                ConnectTimeout = TimeSpan.FromMilliseconds(100)
            })
            .AddIdempotentHttpResilience()
            .ConfigurePrimaryHttpMessageHandler(() => handler);
        await using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("idempotent");
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://example.invalid/payment");
        request.Headers.Add("Idempotency-Key", "payment-operation-123");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(3, handler.Attempts);
    }

    private sealed class CountingHandler : HttpMessageHandler
    {
        public int Attempts { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Attempts++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                RequestMessage = request
            });
        }
    }
}
