using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using ServiceDefaults;

namespace ServiceDefaults.Tests;

public class ConfigurationAndTelemetryTests
{
    [Fact]
    public async Task Production_required_option_validation_names_key_without_printing_value()
    {
        const string unrelatedSecret = "do-not-print-this-value";
        var builder = new HostApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = Environments.Production
        });
        builder.Configuration["UnrelatedSecret"] = unrelatedSecret;
        builder.Logging.ClearProviders();
        builder.AddRequiredConnectionString("orders");
        using var host = builder.Build();

        var exception = await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAsync());

        Assert.Contains("ConnectionStrings:orders", exception.Message);
        Assert.DoesNotContain(unrelatedSecret, exception.Message);
    }

    [Fact]
    public void Sensitive_telemetry_values_and_attributes_are_redacted()
    {
        var text = "Authorization: Bearer super.secret.token user@example.com AccountKey=top-secret; 4111 1111 1111 1111";

        var sanitized = TelemetrySanitizer.RedactText(text);

        Assert.DoesNotContain("super.secret", sanitized);
        Assert.DoesNotContain("user@example.com", sanitized);
        Assert.DoesNotContain("top-secret", sanitized);
        Assert.DoesNotContain("4111", sanitized);
        Assert.True(TelemetrySanitizer.IsSensitiveKey("messaging.message.body"));
        Assert.True(TelemetrySanitizer.IsSensitiveKey("customer.email"));
        Assert.True(TelemetrySanitizer.IsSensitiveKey("psp.credentials"));
    }

    [Fact]
    public void Url_query_strings_are_removed_from_telemetry()
    {
        Assert.Equal(
            "https://example.test/callback",
            TelemetrySanitizer.RedactText("https://example.test/callback?access_token=secret"));
    }
}
