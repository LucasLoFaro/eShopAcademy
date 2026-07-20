using Domain.Payments.Contracts;
using Infrastructure.Configuration;
using Infrastructure.Helpers;
using Infrastructure.Idempotency;
using Infrastructure.Observability;
using Microsoft.AspNetCore.HttpOverrides;
using OpenTelemetry.Metrics;
using ServiceDefaults;
using Psp.Simulator;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults().WithSwagger();
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
builder.Services.AddSingleton<ISignatureHelper, SignatureHelper>();
builder.Services.AddSingleton<IPaymentOperationRegistry, PaymentOperationRegistry>();
builder.Services.AddSingleton<PspPaymentRegistry>();
#pragma warning disable EXTEXP0001
builder.Services.AddHttpClient("webhook", client => client.Timeout = TimeSpan.FromSeconds(5))
    .RemoveAllResilienceHandlers();
#pragma warning restore EXTEXP0001

var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseCors();
app.UseDefaultEndpoints();

app.MapPost("/psp/make-payment", (
    PaymentRequest request,
    HttpRequest httpRequest,
    ISignatureHelper signatureHelper,
    PspPaymentRegistry registry) =>
{
    if (!httpRequest.Headers.TryGetValue("X-Signature", out var signature) ||
        !signatureHelper.VerifyPaymentRequest(request, signature.ToString()))
        return Results.Unauthorized();

    if (!httpRequest.Headers.TryGetValue("Idempotency-Key", out var idempotencyKey) ||
        idempotencyKey.ToString() != request.ExternalId ||
        !Guid.TryParse(request.ExternalId, out _) ||
        request.Amount <= 0 ||
        string.IsNullOrWhiteSpace(request.Currency) ||
        !Uri.TryCreate(request.NotificationUrl, UriKind.Absolute, out var notificationUri) ||
        notificationUri.Scheme is not ("http" or "https"))
        return Results.BadRequest();

    var registration = registry.Register(request, notificationUri);
    if (registration.Conflict)
        return Results.Conflict();

    var payment = registration.Payment;

    var baseUrl = $"{httpRequest.Scheme}://{httpRequest.Host}";

    var response = new PaymentResponse
    {
        Id = payment.Id,
        ExternalId = request.ExternalId,
        Amount = request.Amount,
        Currency = request.Currency,
        Status = payment.Status,
        Url = $"{baseUrl}/psp/payment/{payment.Id}"
    };

    return Results.Ok(response);
});

app.MapGet("/psp/payment/{id}", (string id, PspPaymentRegistry registry) =>
{
    if (!registry.TryGet(id, out var payment))
        return Results.NotFound("Payment not found.");

    var html = $$"""
    <!DOCTYPE html>
    <html lang="en">
    <head>
        <meta charset="UTF-8" />
        <meta name="viewport" content="width=device-width, initial-scale=1.0" />
        <title>PSP Payment - {{payment.Id}}</title>
        <style>
            * { margin: 0; padding: 0; box-sizing: border-box; }
            body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; background: #f0f2f5; display: flex; justify-content: center; align-items: center; min-height: 100vh; }
            .card { background: white; border-radius: 12px; padding: 40px; box-shadow: 0 4px 24px rgba(0,0,0,0.1); max-width: 450px; width: 100%; }
            .card h1 { font-size: 24px; margin-bottom: 8px; color: #1a1a2e; }
            .card .subtitle { color: #666; margin-bottom: 24px; font-size: 14px; }
            .info { background: #f8f9fa; border-radius: 8px; padding: 16px; margin-bottom: 24px; }
            .info .row { display: flex; justify-content: space-between; padding: 8px 0; border-bottom: 1px solid #e9ecef; }
            .info .row:last-child { border-bottom: none; }
            .info .label { color: #666; font-size: 14px; }
            .info .value { font-weight: 600; color: #1a1a2e; }
            .amount-highlight { font-size: 32px; font-weight: 700; color: #1a1a2e; text-align: center; margin: 20px 0; }
            .buttons { display: flex; gap: 12px; }
            .btn { flex: 1; padding: 14px; border: none; border-radius: 8px; font-size: 16px; font-weight: 600; cursor: pointer; transition: opacity 0.2s; }
            .btn:hover { opacity: 0.9; }
            .btn:disabled { opacity: 0.5; cursor: not-allowed; }
            .btn-confirm { background: #28a745; color: white; }
            .btn-reject { background: #dc3545; color: white; }
            .result { text-align: center; padding: 20px; border-radius: 8px; margin-top: 16px; display: none; font-weight: 600; }
            .result.success { background: #d4edda; color: #155724; display: block; }
            .result.failed { background: #f8d7da; color: #721c24; display: block; }
        </style>
    </head>
    <body>
        <div class="card">
            <h1>💳 Payment Confirmation</h1>
            <p class="subtitle">PSP Simulator</p>
            <div class="amount-highlight">{{payment.Amount:F2}} {{payment.Currency}}</div>
            <div class="info">
                <div class="row"><span class="label">Payment ID</span><span class="value">{{payment.Id}}</span></div>
                <div class="row"><span class="label">Order ID</span><span class="value">{{payment.ExternalId}}</span></div>
                <div class="row"><span class="label">Status</span><span class="value" id="status">Pending</span></div>
            </div>
            <div class="buttons" id="buttons">
                <button class="btn btn-confirm" onclick="resolve('confirm')">✅ Confirm Payment</button>
                <button class="btn btn-reject" onclick="resolve('reject')">❌ Reject Payment</button>
            </div>
            <div class="result" id="result"></div>
        </div>
        <script>
            async function resolve(action) {
                document.querySelectorAll('.btn').forEach(b => b.disabled = true);
                try {
                    const res = await fetch(`/psp/payment/{{payment.Id}}/${action}`, { method: 'POST' });
                    const data = await res.json();
                    document.getElementById('status').textContent = data.status;
                    const resultEl = document.getElementById('result');
                    resultEl.textContent = data.status === 'success' ? 'Payment confirmed successfully!' : 'Payment was rejected.';
                    resultEl.className = 'result ' + (data.status === 'success' ? 'success' : 'failed');
                    document.getElementById('buttons').style.display = 'none';
                } catch (e) {
                    alert('Error processing payment: ' + e.message);
                    document.querySelectorAll('.btn').forEach(b => b.disabled = false);
                }
            }
        </script>
    </body>
    </html>
    """;

    return Results.Content(html, "text/html");
});

app.MapPost("/psp/payment/{id}/confirm", (
    string id,
    IHttpClientFactory httpClientFactory,
    ISignatureHelper signatureHelper,
    IPaymentOperationRegistry operations,
    PspPaymentRegistry registry,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
    ResolvePayment(
        id,
        "success",
        null,
        registry,
        httpClientFactory,
        signatureHelper,
        operations,
        logger,
        cancellationToken));

app.MapPost("/psp/payment/{id}/reject", (
    string id,
    IHttpClientFactory httpClientFactory,
    ISignatureHelper signatureHelper,
    IPaymentOperationRegistry operations,
    PspPaymentRegistry registry,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
    ResolvePayment(
        id,
        "cancelled",
        "Payment rejected by user",
        registry,
        httpClientFactory,
        signatureHelper,
        operations,
        logger,
        cancellationToken));

app.Run();

static async Task<IResult> ResolvePayment(
    string id,
    string status,
    string? failureReason,
    PspPaymentRegistry registry,
    IHttpClientFactory httpClientFactory,
    ISignatureHelper signatureHelper,
    IPaymentOperationRegistry operations,
    ILogger logger,
    CancellationToken cancellationToken)
{
    if (!registry.TryGet(id, out var payment))
        return Results.NotFound();

    if (payment.Status != "Pending")
        return Results.Json(new { status = payment.Status });

    var operationKey = $"resolve:{payment.Id}";
    if (!operations.TryBegin(operationKey))
        return Results.Conflict(new { status = "processing" });

    var notification = new PaymentNotification
    {
        Id = payment.Id,
        ExternalId = payment.ExternalId,
        Amount = payment.Amount,
        Currency = payment.Currency,
        Status = status,
        FailureReason = failureReason
    };

    try
    {
        var client = httpClientFactory.CreateClient("webhook");
        using var request = new HttpRequestMessage(HttpMethod.Post, payment.NotificationUrl)
        {
            Content = JsonContent.Create(notification)
        };
        request.Headers.Add("X-Signature", signatureHelper.SignNotificationRequest(notification));
        request.Headers.Add("Idempotency-Key", $"{payment.Id}:{status}");

        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        payment.Status = status;
        PaymentTelemetry.RecordResult("psp_simulator", status);
        logger.LogInformation(
            "Resolved simulated payment {ProviderTransactionId} for order {OrderId} with status {PaymentStatus}",
            payment.Id,
            payment.ExternalId,
            status);
        return Results.Json(new { status });
    }
    catch
    {
        operations.Abandon(operationKey);
        PaymentTelemetry.RecordFault("psp-webhook", "delivery");
        throw;
    }
}

public partial class Program { }
