using Infrastructure.Configuration;
using Health;
using Infrastructure.Helpers;
using Infrastructure.Messaging;
using Infrastructure.Observability;
using Infrastructure.Psp;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using OpenTelemetry.Metrics;
using ServiceDefaults;
using Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults()
    .WithMassTransit();

builder.Services.AddOpenTelemetry().WithMetrics(metrics => metrics.AddMeter(PaymentTelemetry.MeterName));
builder.Services.AddOptions<PaymentSecurityOptions>()
    .Bind(builder.Configuration.GetSection(PaymentSecurityOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<PspClientOptions>()
    .Bind(builder.Configuration.GetSection(PspClientOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddGrpc();
builder.Services.AddProblemDetails();
builder.Services.AddScoped<ISignatureHelper, SignatureHelper>();
builder.Services.AddTransient<IPaymentMessagingClient, PaymentMessagingClient>();

var configuredPspUrl = builder.Configuration["services:eshopacademy-psp-simulator:psp-simulator:0"];
if (!Uri.TryCreate(configuredPspUrl, UriKind.Absolute, out var pspUri) ||
    pspUri.Scheme is not ("http" or "https"))
{
    throw new InvalidOperationException(
        "The PSP base address configuration 'services:eshopacademy-psp-simulator:psp-simulator:0' must be an absolute HTTP(S) URI.");
}

var pspTimeout = TimeSpan.FromSeconds(
    builder.Configuration.GetValue<int?>("Payment:Psp:TimeoutSeconds") ?? 5);

#pragma warning disable EXTEXP0001
builder.Services.AddHttpClient<IPspPaymentClient, PspPaymentClient>(client =>
{
    client.BaseAddress = pspUri;
    client.Timeout = pspTimeout;
}).RemoveAllResilienceHandlers();

builder.Services.AddHttpClient("psp-health", client =>
{
    client.BaseAddress = pspUri;
    client.Timeout = TimeSpan.FromSeconds(2);
}).RemoveAllResilienceHandlers();
#pragma warning restore EXTEXP0001

builder.Services.AddHealthChecks().AddCheck<PspHealthCheck>(
    "psp",
    tags: ["ready"],
    timeout: TimeSpan.FromSeconds(3));

builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureEndpointDefaults(listen => listen.Protocols = HttpProtocols.Http2);
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseDefaultEndpoints();
app.MapGrpcService<PaymentService>();
app.MapGet("/", () => Results.Ok(new { service = "payments-grpc" }));
app.Run();

public partial class Program { }
