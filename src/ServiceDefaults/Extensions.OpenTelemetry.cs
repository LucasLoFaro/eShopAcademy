using System.Diagnostics;
using System.Reflection;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using ServiceDefaults;

namespace Microsoft.Extensions.Hosting;

public static partial class Extensions
{
    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        var options = TelemetryOptions.Resolve(builder.Configuration, builder.Environment);
        builder.Services.AddSingleton(options);

        builder.Services.Configure<LoggerFactoryOptions>(logging =>
            logging.ActivityTrackingOptions = ActivityTrackingOptions.TraceId |
                                              ActivityTrackingOptions.SpanId |
                                              ActivityTrackingOptions.ParentId |
                                              ActivityTrackingOptions.Tags |
                                              ActivityTrackingOptions.Baggage);

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = false;
            logging.IncludeScopes = true;
            logging.ParseStateValues = true;
            logging.AddProcessor(new SensitiveLogRecordProcessor());
            AddLogExporter(logging, options);
        });

        var serviceName = options.ServiceName ?? builder.Environment.ApplicationName;
        var serviceVersion = options.ServiceVersion ??
                             Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ??
                             Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ??
                             "unknown";
        var instanceId = options.ServiceInstanceId ?? $"{serviceName}-{Environment.ProcessId}-{Guid.NewGuid():N}";

        var openTelemetry = builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName, serviceVersion: serviceVersion, serviceInstanceId: instanceId)
                .AddAttributes([
                    new KeyValuePair<string, object>("deployment.environment", builder.Environment.EnvironmentName)
                ]))
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter("MassTransit");
                AddMetricExporter(metrics, options);
            })
            .WithTracing(tracing =>
            {
                tracing.SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(options.SamplingRatio)))
                    .AddSource(builder.Environment.ApplicationName)
                    .AddSource("MassTransit")
                    .AddSource("Npgsql")
                    .AddSource("MongoDB.Driver.Core.Extensions.DiagnosticSources")
                    .AddSource("StackExchange.Redis")
                    .AddAspNetCoreInstrumentation(instrumentation =>
                    {
                        instrumentation.RecordException = true;
                    })
                    .AddGrpcClientInstrumentation()
                    .AddHttpClientInstrumentation(instrumentation =>
                    {
                        instrumentation.RecordException = true;
                    })
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddProcessor(new SensitiveActivityProcessor());
                AddTraceExporter(tracing, options);
            });

        _ = openTelemetry;
        return builder;
    }

    private static void AddTraceExporter(TracerProviderBuilder builder, TelemetryOptions options)
    {
        switch (options.ExportMode)
        {
            case TelemetryExportMode.Console:
                builder.AddConsoleExporter();
                break;
            case TelemetryExportMode.Otlp:
                builder.AddOtlpExporter(exporter => exporter.Endpoint = new Uri(options.OtlpEndpoint!));
                break;
            case TelemetryExportMode.AzureMonitor:
                builder.AddAzureMonitorTraceExporter(exporter =>
                    exporter.ConnectionString = options.ApplicationInsightsConnectionString);
                break;
        }
    }

    private static void AddMetricExporter(MeterProviderBuilder builder, TelemetryOptions options)
    {
        switch (options.ExportMode)
        {
            case TelemetryExportMode.Console:
                builder.AddConsoleExporter();
                break;
            case TelemetryExportMode.Otlp:
                builder.AddOtlpExporter(exporter => exporter.Endpoint = new Uri(options.OtlpEndpoint!));
                break;
            case TelemetryExportMode.AzureMonitor:
                builder.AddAzureMonitorMetricExporter(exporter =>
                    exporter.ConnectionString = options.ApplicationInsightsConnectionString);
                break;
        }
    }

    private static void AddLogExporter(OpenTelemetryLoggerOptions builder, TelemetryOptions options)
    {
        switch (options.ExportMode)
        {
            case TelemetryExportMode.Console:
                builder.AddConsoleExporter();
                break;
            case TelemetryExportMode.Otlp:
                builder.AddOtlpExporter(exporter => exporter.Endpoint = new Uri(options.OtlpEndpoint!));
                break;
            case TelemetryExportMode.AzureMonitor:
                builder.AddAzureMonitorLogExporter(exporter =>
                    exporter.ConnectionString = options.ApplicationInsightsConnectionString);
                break;
        }
    }
}
