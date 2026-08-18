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

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("shipping-secret-canary")]
    public async Task Required_shipping_secret_rejects_missing_empty_or_invalid_values_without_printing_them(
        string? secret)
    {
        const string key = "Shipping:SignatureSecret";
        var builder = new HostApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = Environments.Production
        });
        if (secret is not null)
        {
            builder.Configuration[key] = secret;
        }

        builder.Logging.ClearProviders();
        builder.AddRequiredSecret(key);
        using var host = builder.Build();

        var exception = await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAsync());

        Assert.Contains(key, exception.Message);
        if (!string.IsNullOrEmpty(secret))
        {
            Assert.DoesNotContain(secret, exception.Message);
        }
    }

    [Fact]
    public async Task Required_shipping_secret_accepts_a_valid_value()
    {
        const string key = "Shipping:SignatureSecret";
        var builder = new HostApplicationBuilder();
        builder.Configuration[key] = new string('s', 32);
        builder.Logging.ClearProviders();
        builder.AddRequiredSecret(key);
        using var host = builder.Build();

        await host.StartAsync();
        await host.StopAsync();
    }

    [Fact]
    public void AppHost_shares_one_secret_parameter_with_all_shipping_processes()
    {
        var root = RepositoryRoot();
        var environmentSetup = File.ReadAllText(Path.Combine(
            root,
            "src",
            "AppHost",
            "Setup",
            "EnvironmentSetup.cs"));
        var shippingExtensions = File.ReadAllText(Path.Combine(
            root,
            "src",
            "AppHost",
            "Setup",
            "Extensions",
            "ShippingExtensions.cs"));

        Assert.Contains(
            "builder.AddParameter(\"shipping-signature-secret\", secret: true)",
            environmentSetup);
        Assert.Equal(
            3,
            shippingExtensions.Split(
                ".WithEnvironment(\"Shipping__SignatureSecret\", shippingSignatureSecret)",
                StringSplitOptions.None).Length - 1);
        Assert.DoesNotContain(
            ".WithEnvironment(\"Shipping__SignatureSecret\", \"",
            shippingExtensions);
    }

    [Fact]
    public void Every_shipping_signature_participant_requires_the_shared_secret()
    {
        var shippingRoot = Path.Combine(RepositoryRoot(), "src", "Shipping");
        var programs = new[]
        {
            Path.Combine(shippingRoot, "Shipping.Api", "Program.cs"),
            Path.Combine(shippingRoot, "Shipping.Service", "Program.cs"),
            Path.Combine(shippingRoot, "Shipping.Simulator", "Program.cs")
        };

        Assert.All(
            programs,
            program => Assert.Contains(
                "AddRequiredSecret(\"Shipping:SignatureSecret\")",
                File.ReadAllText(program)));
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

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Directory.Packages.props")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
