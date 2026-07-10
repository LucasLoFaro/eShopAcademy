using Domain.Payments.Contracts;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHttpClient("webhook");

var app = builder.Build();

var payments = new ConcurrentDictionary<string, PendingPayment>();

app.MapPost("/psp/make-payment", (PaymentRequest request, HttpRequest httpRequest) =>
{
    var id = Guid.NewGuid().ToString();

    payments[id] = new PendingPayment
    {
        Id = id,
        ExternalId = request.ExternalId,
        Amount = request.Amount,
        Currency = request.Currency,
        NotificationUrl = request.NotificationUrl,
        Signature = httpRequest.Headers["X-Signature"].ToString()
    };

    var baseUrl = $"{httpRequest.Scheme}://{httpRequest.Host}";

    var response = new PaymentResponse
    {
        Id = id,
        ExternalId = request.ExternalId,
        Amount = request.Amount,
        Currency = request.Currency,
        Status = "Pending",
        Url = $"{baseUrl}/psp/payment/{id}"
    };

    return Results.Ok(response);
});

app.MapGet("/psp/payment/{id}", (string id) =>
{
    if (!payments.TryGetValue(id, out var payment))
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

app.MapPost("/psp/payment/{id}/confirm", async (string id, IHttpClientFactory httpClientFactory) =>
{
    if (!payments.TryGetValue(id, out var payment))
        return Results.NotFound("Payment not found.");

    var notification = new PaymentNotification
    {
        Id = payment.Id,
        ExternalId = payment.ExternalId,
        Amount = payment.Amount,
        Currency = payment.Currency,
        Status = "success"
    };

    await SendWebhookNotification(httpClientFactory, payment, notification);

    return Results.Json(new { status = "success" });
});

app.MapPost("/psp/payment/{id}/reject", async (string id, IHttpClientFactory httpClientFactory) =>
{
    if (!payments.TryGetValue(id, out var payment))
        return Results.NotFound("Payment not found.");

    var notification = new PaymentNotification
    {
        Id = payment.Id,
        ExternalId = payment.ExternalId,
        Amount = payment.Amount,
        Currency = payment.Currency,
        Status = "cancelled",
        FailureReason = "Payment rejected by user"
    };

    await SendWebhookNotification(httpClientFactory, payment, notification);

    return Results.Json(new { status = "cancelled" });
});

app.Run();

static async Task SendWebhookNotification(IHttpClientFactory httpClientFactory, PendingPayment payment, PaymentNotification notification)
{
    var client = httpClientFactory.CreateClient("webhook");
    var json = JsonSerializer.Serialize(notification);
    using var request = new HttpRequestMessage(HttpMethod.Post, payment.NotificationUrl)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };
    request.Headers.Add("X-Signature", "Signature");

    await client.SendAsync(request);
}

record PendingPayment
{
    public string Id { get; init; } = default!;
    public string ExternalId { get; init; } = default!;
    public double Amount { get; init; }
    public string Currency { get; init; } = default!;
    public string NotificationUrl { get; init; } = default!;
    public string Signature { get; init; } = default!;
}
