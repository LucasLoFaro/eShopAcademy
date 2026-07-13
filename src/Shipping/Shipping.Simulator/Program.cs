using System.Text;
using System.Text.Json;
using Domain.Shipping.Contracts.Responses;
using Shipping.Simulator.Storage;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHttpClient("webhook");

var redisConnectionString = builder.Configuration.GetConnectionString("redis")
    ?? throw new InvalidOperationException("Redis connection string 'redis' is missing.");
var redisDatabase = builder.Configuration.GetValue<int?>("ShippingSimulator__RedisDatabase") ?? 1;

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var options = ConfigurationOptions.Parse(redisConnectionString);
    options.AbortOnConnectFail = false;
    return ConnectionMultiplexer.Connect(options);
});
builder.Services.AddSingleton(sp =>
    new ShipmentStore(sp.GetRequiredService<IConnectionMultiplexer>(), redisDatabase));

var app = builder.Build();

// Shipping provider endpoints (called by the Shipping service)
app.MapPost("/shipping/schedule", async (ShippingScheduleRequest request, HttpRequest httpRequest, IHttpClientFactory httpClientFactory, ShipmentStore store) =>
{
    var id = Guid.NewGuid();
    var trackingNumber = $"SIM-{Guid.NewGuid().ToString()[..8].ToUpper()}";
    var baseUrl = $"{httpRequest.Scheme}://{httpRequest.Host}";

    var shipment = new SimulatedShipment
    {
        ShipmentId = id,
        OrderId = request.OrderId,
        OriginAddress = request.SellerAddress ?? "Seller Warehouse, 123 Commerce St, New York, NY 10001",
        DestinationAddress = request.Address?.Street ?? "Unknown destination",
        Carrier = "ShipSim",
        TrackingNumber = trackingNumber,
        Status = "accepted",
        WebhookUrl = $"https+http://eshopacademy-shipping-api/api/shipping/webhook",
        CreatedAt = DateTime.UtcNow,
        EstimatedDelivery = DateTime.UtcNow.AddDays(3)
    };

    await store.SaveAsync(shipment);

    // Notify the shipping service so it records the initial history entry
    await SendWebhook(httpClientFactory, shipment, "accepted");

    return Results.Ok(new ScheduleShippingResponse(
        id,
        request.OrderId,
        "ShipSim",
        trackingNumber,
        "accepted"));
});

app.MapPost("/shipping/confirm-pickup", async (PickupConfirmRequest request, IHttpClientFactory httpClientFactory, ShipmentStore store) =>
{
    var shipment = await store.FindByShipmentIdAsync(request.ShippingId);
    if (shipment is null)
        return Results.NotFound();

    shipment.Status = "picked_up";
    shipment.PickedUpAt = DateTime.UtcNow;
    shipment.StatusHistory.Add(new StatusHistoryEntry { Status = "picked_up", OccurredAt = DateTime.UtcNow });
    await store.SaveAsync(shipment);

    // Notify the shipping service so pickup is reflected in history
    await SendWebhook(httpClientFactory, shipment, "picked_up");

    return Results.Ok();
});

app.MapGet("/shipping/{orderId}/history", async (Guid orderId, ShipmentStore store) =>
{
    var shipment = await store.FindByOrderIdAsync(orderId);
    if (shipment is null)
        return Results.Ok(Array.Empty<ShippingStatusResponse>());

    var history = shipment.StatusHistory.Select(h =>
        new ShippingStatusResponse(shipment.ShipmentId, shipment.OrderId, h.Status, shipment.TrackingNumber, shipment.Carrier, h.OccurredAt)).ToList();

    return Results.Ok(history);
});

// Simulator management endpoints (called by the simulator frontend)
app.MapGet("/simulator/shipments", async (ShipmentStore store) =>
    Results.Ok((await store.GetAllAsync()).OrderByDescending(s => s.CreatedAt)));

app.MapGet("/simulator/shipments/{id}", async (string id, ShipmentStore store) =>
{
    var shipment = await store.GetByIdAsync(id);
    return shipment is not null ? Results.Ok(shipment) : Results.NotFound();
});

app.MapPost("/simulator/shipments/{id}/transition", async (string id, StatusTransitionRequest request, IHttpClientFactory httpClientFactory, ShipmentStore store) =>
{
    var shipment = await store.GetByIdAsync(id);
    if (shipment is null)
        return Results.NotFound("Shipment not found.");

    shipment.Status = request.Status;
    shipment.StatusHistory.Add(new StatusHistoryEntry { Status = request.Status, OccurredAt = DateTime.UtcNow });

    if (request.Status == "shipped")
        shipment.PickedUpAt = DateTime.UtcNow;
    else if (request.Status == "delivered")
        shipment.DeliveredAt = DateTime.UtcNow;

    await store.SaveAsync(shipment);

    // Call shipping webhook
    await SendWebhook(httpClientFactory, shipment, request.Status);

    return Results.Ok(shipment);
});

app.MapPost("/simulator/shipments/{id}/report-issue", async (string id, ReportIssueRequest request, IHttpClientFactory httpClientFactory, ShipmentStore store) =>
{
    var shipment = await store.GetByIdAsync(id);
    if (shipment is null)
        return Results.NotFound("Shipment not found.");

    shipment.Status = "failed";
    shipment.IssueDetails = request.Details;
    shipment.StatusHistory.Add(new StatusHistoryEntry { Status = "failed", OccurredAt = DateTime.UtcNow });

    await store.SaveAsync(shipment);

    await SendWebhook(httpClientFactory, shipment, "failed");

    return Results.Ok(shipment);
});

// Frontend
app.MapGet("/", (HttpRequest httpRequest) =>
{
    var baseUrl = $"{httpRequest.Scheme}://{httpRequest.Host}";
    return Results.Content(GetDashboardHtml(baseUrl), "text/html");
});

app.MapGet("/shipment/{id}", (string id, HttpRequest httpRequest) =>
{
    var baseUrl = $"{httpRequest.Scheme}://{httpRequest.Host}";
    return Results.Content(GetShipmentDetailHtml(baseUrl, id), "text/html");
});

app.MapGet("/shipment/by-order/{orderId:guid}", async (Guid orderId, HttpRequest httpRequest, ShipmentStore store) =>
{
    var shipment = await store.FindByOrderIdAsync(orderId);
    if (shipment is null)
        return Results.NotFound("No shipment found for this order.");

    var baseUrl = $"{httpRequest.Scheme}://{httpRequest.Host}";
    return Results.Content(GetShipmentDetailHtml(baseUrl, shipment.ShipmentId.ToString()), "text/html");
});

app.UseDefaultEndpoints();
app.Run();

static async Task SendWebhook(IHttpClientFactory httpClientFactory, SimulatedShipment shipment, string status)
{
    var client = httpClientFactory.CreateClient("webhook");
    var payload = new
    {
        ShippingId = shipment.ShipmentId,
        OrderId = shipment.OrderId,
        Status = status,
        TrackingNumber = shipment.TrackingNumber,
        Carrier = shipment.Carrier,
        OccurredAt = DateTime.UtcNow
    };

    var json = JsonSerializer.Serialize(payload);
    using var request = new HttpRequestMessage(HttpMethod.Post, shipment.WebhookUrl)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };
    request.Headers.Add("X-Signature", "Signature");

    try
    {
        await client.SendAsync(request);
    }
    catch
    {
        // Webhook delivery failure is non-fatal for the simulator
    }
}

static string GetDashboardHtml(string baseUrl) => $$"""
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Shipping Provider Simulator</title>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; background: #f0f2f5; padding: 24px; }
        .container { max-width: 1000px; margin: 0 auto; }
        h1 { font-size: 28px; color: #1a1a2e; margin-bottom: 8px; }
        .subtitle { color: #666; margin-bottom: 24px; }
        .card { background: white; border-radius: 12px; padding: 20px; box-shadow: 0 2px 12px rgba(0,0,0,0.08); margin-bottom: 16px; }
        .empty { text-align: center; padding: 60px; color: #999; }
        table { width: 100%; border-collapse: collapse; }
        th { text-align: left; padding: 12px; font-size: 12px; text-transform: uppercase; color: #666; border-bottom: 2px solid #e9ecef; }
        td { padding: 12px; border-bottom: 1px solid #f1f5f9; }
        .badge { display: inline-block; padding: 4px 10px; border-radius: 20px; font-size: 12px; font-weight: 600; }
        .badge-accepted { background: #fef3c7; color: #92400e; }
        .badge-shipped, .badge-picked_up { background: #dbeafe; color: #1e40af; }
        .badge-out_for_delivery { background: #e0e7ff; color: #3730a3; }
        .badge-delivered { background: #d1fae5; color: #065f46; }
        .badge-failed { background: #fee2e2; color: #991b1b; }
        a { color: #4f46e5; text-decoration: none; font-weight: 500; }
        a:hover { text-decoration: underline; }
        .refresh-btn { background: #4f46e5; color: white; border: none; padding: 8px 16px; border-radius: 8px; cursor: pointer; font-size: 14px; }
        .refresh-btn:hover { background: #4338ca; }
        .header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 24px; }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <div>
                <h1>🚚 Shipping Provider Simulator</h1>
                <p class="subtitle">Manage simulated shipments and trigger status updates</p>
            </div>
            <button class="refresh-btn" onclick="loadShipments()">↻ Refresh</button>
        </div>
        <div class="card">
            <div id="content"><p class="empty">Loading shipments...</p></div>
        </div>
    </div>
    <script>
        function statusLabel(status) {
            switch (status) {
                case 'accepted': return 'Accepted';
                case 'picked_up': return 'In transit';
                case 'shipped': return 'In transit';
                case 'out_for_delivery': return 'Out for delivery';
                case 'delivered': return 'Delivered';
                case 'failed': return 'Failed';
                case 'cancelled': return 'Cancelled';
                default: return status.replace(/_/g, ' ');
            }
        }
        async function loadShipments() {
            const res = await fetch('/simulator/shipments');
            const shipments = await res.json();
            const container = document.getElementById('content');
            if (shipments.length === 0) {
                container.innerHTML = '<p class="empty">📦 No shipments yet. Shipments will appear here when orders are placed.</p>';
                return;
            }
            let html = '<table><thead><tr><th>Tracking</th><th>Order</th><th>Destination</th><th>Status</th><th>ETA</th><th></th></tr></thead><tbody>';
            for (const s of shipments) {
                const eta = new Date(s.estimatedDelivery).toLocaleDateString();
                html += `<tr>
                    <td><code>${s.trackingNumber}</code></td>
                    <td>${s.orderId.slice(0,8)}...</td>
                    <td>${s.destinationAddress.slice(0,30)}${s.destinationAddress.length > 30 ? '...' : ''}</td>
                    <td><span class="badge badge-${s.status}">${statusLabel(s.status)}</span></td>
                    <td>${eta}</td>
                    <td><a href="/shipment/${s.shipmentId}">Details →</a></td>
                </tr>`;
            }
            html += '</tbody></table>';
            container.innerHTML = html;
        }
        loadShipments();
        setInterval(loadShipments, 5000);
    </script>
</body>
</html>
""";

static string GetShipmentDetailHtml(string baseUrl, string id) => $$"""
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Shipment Details</title>
    <link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css" />
    <script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"></script>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; background: #f0f2f5; padding: 24px; }
        .container { max-width: 1000px; margin: 0 auto; }
        h1 { font-size: 24px; color: #1a1a2e; margin-bottom: 4px; }
        .subtitle { color: #666; margin-bottom: 24px; font-size: 14px; }
        .card { background: white; border-radius: 12px; padding: 20px; box-shadow: 0 2px 12px rgba(0,0,0,0.08); margin-bottom: 16px; }
        .grid { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; }
        .info-row { margin-bottom: 12px; }
        .info-label { font-size: 12px; color: #666; text-transform: uppercase; margin-bottom: 2px; }
        .info-value { font-weight: 600; color: #1a1a2e; }
        #map { height: 300px; border-radius: 8px; margin-bottom: 16px; }
        .badge { display: inline-block; padding: 4px 10px; border-radius: 20px; font-size: 12px; font-weight: 600; }
        .badge-accepted { background: #fef3c7; color: #92400e; }
        .badge-shipped, .badge-picked_up { background: #dbeafe; color: #1e40af; }
        .badge-out_for_delivery { background: #e0e7ff; color: #3730a3; }
        .badge-delivered { background: #d1fae5; color: #065f46; }
        .badge-failed { background: #fee2e2; color: #991b1b; }
        .btn { padding: 10px 18px; border: none; border-radius: 8px; font-size: 14px; font-weight: 600; cursor: pointer; transition: all 0.2s; }
        .btn:hover { opacity: 0.9; transform: translateY(-1px); }
        .btn:disabled { opacity: 0.5; cursor: not-allowed; transform: none; }
        .btn-pickup { background: #3b82f6; color: white; }
        .btn-transit { background: #8b5cf6; color: white; }
        .btn-deliver { background: #10b981; color: white; }
        .btn-issue { background: #ef4444; color: white; }
        .actions { display: flex; gap: 10px; flex-wrap: wrap; margin-top: 16px; }
        .back { display: inline-block; margin-bottom: 16px; color: #4f46e5; text-decoration: none; font-weight: 500; }
        .back:hover { text-decoration: underline; }
        .timeline { margin-top: 16px; }
        .timeline-item { display: flex; gap: 12px; padding: 8px 0; border-left: 3px solid #e5e7eb; padding-left: 16px; margin-left: 8px; }
        .timeline-item.active { border-left-color: #4f46e5; }
        .timeline-time { color: #666; font-size: 12px; min-width: 140px; }
        .timeline-status { font-weight: 600; }
        .eta { background: #f0fdf4; border: 1px solid #bbf7d0; border-radius: 8px; padding: 12px 16px; display: flex; align-items: center; gap: 8px; }
        .eta-label { font-size: 12px; color: #166534; text-transform: uppercase; }
        .eta-value { font-size: 18px; font-weight: 700; color: #166534; }
        .modal-overlay { display: none; position: fixed; inset: 0; background: rgba(0,0,0,0.4); z-index: 1000; justify-content: center; align-items: center; }
        .modal-overlay.active { display: flex; }
        .modal { background: white; border-radius: 12px; padding: 24px; max-width: 400px; width: 100%; }
        .modal h3 { margin-bottom: 12px; }
        .modal textarea { width: 100%; border: 1px solid #d1d5db; border-radius: 8px; padding: 8px; font-size: 14px; resize: vertical; min-height: 80px; }
        .modal-actions { display: flex; gap: 8px; justify-content: flex-end; margin-top: 16px; }
        .btn-cancel { background: #f3f4f6; color: #374151; }
    </style>
</head>
<body>
    <div class="container">
        <a href="/" class="back">← Back to all shipments</a>
        <div id="detail">
            <p style="color:#666">Loading shipment details...</p>
        </div>
    </div>

    <div class="modal-overlay" id="issueModal">
        <div class="modal">
            <h3>⚠️ Report Shipping Issue</h3>
            <p style="color:#666; font-size:14px; margin-bottom:12px;">This will mark the shipment as failed and notify the system.</p>
            <textarea id="issueDetails" placeholder="Describe the issue (e.g., package lost, damaged, address not found...)"></textarea>
            <div class="modal-actions">
                <button class="btn btn-cancel" onclick="closeIssueModal()">Cancel</button>
                <button class="btn btn-issue" onclick="submitIssue()">Report Issue</button>
            </div>
        </div>
    </div>

    <script>
        const shipmentId = '{{id}}';
        let currentShipment = null;

        async function loadShipment() {
            const res = await fetch(`/simulator/shipments/${shipmentId}`);
            if (!res.ok) { document.getElementById('detail').innerHTML = '<p>Shipment not found.</p>'; return; }
            currentShipment = await res.json();
            render();
        }

        function render() {
            const s = currentShipment;
            const eta = new Date(s.estimatedDelivery);
            const now = new Date();
            const hoursLeft = Math.max(0, Math.round((eta - now) / 3600000));
            const isFinal = s.status === 'delivered' || s.status === 'failed';
            const etaHtml = isFinal ? '' : `
                            <div class="info-row">
                                <div class="eta">
                                    <div><div class="eta-label">Estimated Delivery</div><div class="eta-value">~${hoursLeft}h (${eta.toLocaleDateString()})</div></div>
                                </div>
                            </div>`;
            const originAddress = s.originAddress && s.originAddress.trim() ? s.originAddress : DEFAULT_SELLER_ADDRESS;

            let html = `
                <h1>📦 Shipment ${s.trackingNumber}</h1>
                <p class="subtitle">Order ${s.orderId}</p>

                <div class="card">
                    <div id="map"></div>
                    <div class="grid">
                        <div>
                            <div class="info-row"><div class="info-label">Origin (Seller)</div><div class="info-value">${originAddress}</div></div>
                            <div class="info-row"><div class="info-label">Destination</div><div class="info-value">${s.destinationAddress}</div></div>
                        </div>
                        <div>
                            <div class="info-row"><div class="info-label">Carrier</div><div class="info-value">${s.carrier}</div></div>
                            <div class="info-row"><div class="info-label">Status</div><div class="info-value"><span class="badge badge-${s.status}">${statusLabel(s.status)}</span></div></div>
                            ${etaHtml}
                        </div>
                    </div>

                    <div class="actions">${actionButtons(s.status)}</div>
                </div>

                <div class="card">
                    <h3 style="margin-bottom:12px">Status History</h3>
                    <div class="timeline">
                        ${s.statusHistory.map((h, i) => `
                            <div class="timeline-item ${i === s.statusHistory.length - 1 ? 'active' : ''}">
                                <span class="timeline-time">${new Date(h.occurredAt).toLocaleString()}</span>
                                <span class="timeline-status">${statusLabel(h.status)}</span>
                            </div>
                        `).join('')}
                    </div>
                </div>
            `;
            document.getElementById('detail').innerHTML = html;
            initMap(s);
        }

        const DEFAULT_SELLER_ADDRESS = 'Seller Warehouse, 123 Commerce St, New York, NY 10001';

        function statusLabel(status) {
            switch (status) {
                case 'accepted': return 'Accepted';
                case 'picked_up': return 'In transit';
                case 'shipped': return 'In transit';
                case 'out_for_delivery': return 'Out for delivery';
                case 'delivered': return 'Delivered';
                case 'failed': return 'Failed';
                case 'cancelled': return 'Cancelled';
                default: return status.replace(/_/g, ' ');
            }
        }

        function actionButtons(status) {
            const buttons = [];
            if (status === 'accepted') {
                buttons.push(`<button class="btn btn-pickup" onclick="transition('shipped')">🚛 Mark In Transit</button>`);
            } else if (status === 'shipped' || status === 'picked_up') {
                buttons.push(`<button class="btn btn-transit" onclick="transition('out_for_delivery')">📍 Out for Delivery</button>`);
            } else if (status === 'out_for_delivery') {
                buttons.push(`<button class="btn btn-deliver" onclick="transition('delivered')">✅ Delivered</button>`);
            }
            if (status !== 'delivered' && status !== 'failed') {
                buttons.push(`<button class="btn btn-issue" onclick="openIssueModal()">⚠️ Report Issue</button>`);
            }
            return buttons.join('');
        }

        async function geocode(address) {
            if (!address || !address.trim()) return null;
            try {
                const res = await fetch(`https://nominatim.openstreetmap.org/search?format=json&limit=1&q=${encodeURIComponent(address)}`);
                const data = await res.json();
                if (data && data.length > 0) {
                    return [parseFloat(data[0].lat), parseFloat(data[0].lon)];
                }
            } catch (e) { /* geocoding is best-effort */ }
            return null;
        }

        async function initMap(s) {
            const originAddress = s.originAddress && s.originAddress.trim() ? s.originAddress : DEFAULT_SELLER_ADDRESS;

            // Resolve real coordinates from the addresses, falling back to NYC demo points
            const origin = (await geocode(originAddress)) || [40.7128, -74.0060];
            const dest = (await geocode(s.destinationAddress)) || [40.7580, -73.9855];

            const center = [(origin[0] + dest[0]) / 2, (origin[1] + dest[1]) / 2];
            const map = L.map('map').setView(center, 12);
            L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                attribution: '© OpenStreetMap contributors'
            }).addTo(map);

            L.marker(origin).addTo(map).bindPopup('<b>Origin</b><br>' + originAddress).openPopup();
            L.marker(dest).addTo(map).bindPopup('<b>Destination</b><br>' + s.destinationAddress);
            L.polyline([origin, dest], { color: '#4f46e5', weight: 3, dashArray: '8,8' }).addTo(map);

            map.fitBounds([origin, dest], { padding: [40, 40] });
        }

        async function transition(status) {
            const res = await fetch(`/simulator/shipments/${shipmentId}/transition`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ status })
            });
            if (res.ok) {
                currentShipment = await res.json();
                render();
            }
        }

        function openIssueModal() { document.getElementById('issueModal').classList.add('active'); }
        function closeIssueModal() { document.getElementById('issueModal').classList.remove('active'); }

        async function submitIssue() {
            const details = document.getElementById('issueDetails').value;
            const res = await fetch(`/simulator/shipments/${shipmentId}/report-issue`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ details })
            });
            if (res.ok) {
                currentShipment = await res.json();
                closeIssueModal();
                render();
            }
        }

        loadShipment();
    </script>
</body>
</html>
""";

// Request/response records
record ShippingScheduleRequest
{
    public Guid OrderId { get; init; }
    public AddressInfo? Address { get; init; }
    public string? SellerAddress { get; init; }
}

record AddressInfo
{
    public string Street { get; init; } = string.Empty;
}

record StatusTransitionRequest
{
    public string Status { get; init; } = string.Empty;
}

record ReportIssueRequest
{
    public string Details { get; init; } = string.Empty;
}

record PickupConfirmRequest
{
    public Guid ShippingId { get; init; }
    public Guid OrderId { get; init; }
    public DateTime ReadyAt { get; init; }
}
