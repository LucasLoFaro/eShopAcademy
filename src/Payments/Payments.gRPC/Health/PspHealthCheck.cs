using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Health;

public sealed class PspHealthCheck(IHttpClientFactory httpClientFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = httpClientFactory.CreateClient("psp-health");
            using var response = await client.GetAsync("/alive", cancellationToken);
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("PSP is unavailable.");
        }
        catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("PSP is unavailable.");
        }
    }
}
