using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ServiceDefaults;

public enum TelemetryExportMode
{
    None,
    Console,
    Otlp,
    AzureMonitor
}

public sealed class TelemetryOptions
{
    public const string SectionName = "Telemetry";

    public TelemetryExportMode ExportMode { get; set; }
    public double SamplingRatio { get; set; } = 1.0;
    public string? ServiceName { get; set; }
    public string? ServiceVersion { get; set; }
    public string? ServiceInstanceId { get; set; }
    public string? OtlpEndpoint { get; set; }
    public string? ApplicationInsightsConnectionString { get; set; }

    internal static TelemetryOptions Resolve(IConfiguration configuration, IHostEnvironment environment)
    {
        var section = configuration.GetSection(SectionName);
        var options = section.Get<TelemetryOptions>() ?? new TelemetryOptions();

        options.OtlpEndpoint ??= configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
        options.ApplicationInsightsConnectionString ??= configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];

        if (string.IsNullOrWhiteSpace(section[nameof(ExportMode)]))
        {
            options.ExportMode = !string.IsNullOrWhiteSpace(options.OtlpEndpoint)
                ? TelemetryExportMode.Otlp
                : !string.IsNullOrWhiteSpace(options.ApplicationInsightsConnectionString)
                    ? TelemetryExportMode.AzureMonitor
                    : environment.IsDevelopment()
                        ? TelemetryExportMode.Console
                        : TelemetryExportMode.None;
        }

        var failures = new List<string>();
        if (!Enum.IsDefined(options.ExportMode))
        {
            failures.Add("Telemetry:ExportMode must be None, Console, Otlp, or AzureMonitor.");
        }

        if (options.SamplingRatio is <= 0 or > 1)
        {
            failures.Add("Telemetry:SamplingRatio must be greater than 0 and no greater than 1.");
        }

        if (options.ExportMode == TelemetryExportMode.Otlp &&
            (!Uri.TryCreate(options.OtlpEndpoint, UriKind.Absolute, out var endpoint) ||
             (endpoint.Scheme != Uri.UriSchemeHttp && endpoint.Scheme != Uri.UriSchemeHttps)))
        {
            failures.Add("Telemetry:OtlpEndpoint (or OTEL_EXPORTER_OTLP_ENDPOINT) must be an absolute HTTP or HTTPS URI when OTLP export is selected.");
        }

        if (options.ExportMode == TelemetryExportMode.AzureMonitor &&
            string.IsNullOrWhiteSpace(options.ApplicationInsightsConnectionString))
        {
            failures.Add("Telemetry:ApplicationInsightsConnectionString (or APPLICATIONINSIGHTS_CONNECTION_STRING) is required when AzureMonitor export is selected.");
        }

        if (failures.Count > 0)
        {
            throw new OptionsValidationException(SectionName, typeof(TelemetryOptions), failures);
        }

        return options;
    }
}
