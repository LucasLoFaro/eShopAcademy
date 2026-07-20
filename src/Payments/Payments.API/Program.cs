using Domain.Payments.Contracts;
using Infrastructure.Configuration;
using Infrastructure.Helpers;
using Infrastructure.Idempotency;
using Infrastructure.Messaging;
using Infrastructure.Observability;
using Microsoft.AspNetCore.HttpOverrides;
using OpenTelemetry.Metrics;
using ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults()
    .WithMassTransit()
    .WithSwagger();

builder.Services.AddOpenTelemetry().WithMetrics(metrics => metrics.AddMeter(PaymentTelemetry.MeterName));
builder.Services.AddOptions<PaymentSecurityOptions>()
    .Bind(builder.Configuration.GetSection(PaymentSecurityOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddProblemDetails();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    if (origins.Length > 0)
        policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
}));
builder.Services.AddSingleton<IPaymentOperationRegistry, PaymentOperationRegistry>();
builder.Services.AddScoped<IPaymentMessagingClient, PaymentMessagingClient>();
builder.Services.AddScoped<ISignatureHelper, SignatureHelper>();

var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseCors();
app.UseDefaultEndpoints();

app.MapPost("/api/payments/webhook", async (
    PaymentNotification notification,
    HttpRequest request,
    ISignatureHelper signatureHelper,
    IPaymentMessagingClient messaging,
    IPaymentOperationRegistry operations,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    if (!request.Headers.TryGetValue("X-Signature", out var suppliedSignature) ||
        !signatureHelper.VerifyWebhookSignature(notification, suppliedSignature.ToString()))
    {
        PaymentTelemetry.RecordResult("webhook", "unauthorized");
        return Results.Unauthorized();
    }

    if (!Guid.TryParse(notification.ExternalId, out var orderId) ||
        string.IsNullOrWhiteSpace(notification.Id) ||
        string.IsNullOrWhiteSpace(notification.Status))
    {
        PaymentTelemetry.RecordResult("webhook", "invalid");
        return Results.BadRequest();
    }

    var status = notification.Status.ToLowerInvariant();
    if (status is not ("success" or "failed" or "cancelled"))
    {
        logger.LogInformation(
            "Ignoring payment notification with unsupported status {PaymentStatus} for order {OrderId}",
            status,
            orderId);
        PaymentTelemetry.RecordResult("webhook", "ignored");
        return Results.Ok();
    }

    var operationKey = $"webhook:{orderId:N}:{notification.Id}:{status}";
    if (!operations.TryBegin(operationKey))
    {
        logger.LogInformation(
            "Ignoring duplicate payment notification for order {OrderId} and provider transaction {ProviderTransactionId}",
            orderId,
            notification.Id);
        PaymentTelemetry.RecordResult("webhook", "duplicate");
        return Results.Ok();
    }

    try
    {
        if (status == "success")
        {
            await messaging.SendPaymentCompleted(orderId, notification.Id, cancellationToken);
            PaymentTelemetry.RecordResult("webhook", "completed");
        }
        else
        {
            await messaging.SendPaymentFailed(
                orderId,
                notification.Id,
                notification.FailureReason ?? "Payment provider rejected the payment",
                cancellationToken);
            PaymentTelemetry.RecordResult("webhook", "failed");
        }
    }
    catch
    {
        operations.Abandon(operationKey);
        PaymentTelemetry.RecordFault("webhook", "publish");
        throw;
    }

    return Results.Ok();
}).WithName("ProcessPaymentNotification");

app.Run();

public partial class Program { }
