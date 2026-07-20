using System.Text.Json;
using StackExchange.Redis;

namespace Shipping.Simulator.Storage;

public sealed class ShipmentStore
{
    private const string ShipmentPrefix = "shipment:";
    private const string ShipmentIndexKey = "shipment:index";

    private readonly IDatabase _db;

    public ShipmentStore(IConnectionMultiplexer connection, int database)
    {
        _db = connection.GetDatabase(database);
    }

    public async Task SaveAsync(SimulatedShipment shipment, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(shipment);
        await _db.StringSetAsync(ShipmentPrefix + shipment.ShipmentId, json).WaitAsync(cancellationToken);
        await _db.SetAddAsync(ShipmentIndexKey, shipment.ShipmentId.ToString()).WaitAsync(cancellationToken);
    }

    public async Task<bool> TryCreateAsync(SimulatedShipment shipment, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(shipment);
        var created = await _db.StringSetAsync(
            ShipmentPrefix + shipment.ShipmentId,
            json,
            when: When.NotExists).WaitAsync(cancellationToken);
        if (created)
            await _db.SetAddAsync(ShipmentIndexKey, shipment.ShipmentId.ToString()).WaitAsync(cancellationToken);
        return created;
    }

    public async Task<SimulatedShipment?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var json = await _db.StringGetAsync(ShipmentPrefix + id).WaitAsync(cancellationToken);
        return json.IsNullOrEmpty ? null : JsonSerializer.Deserialize<SimulatedShipment>((string)json!);
    }

    public async Task<IReadOnlyList<SimulatedShipment>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var ids = await _db.SetMembersAsync(ShipmentIndexKey).WaitAsync(cancellationToken);
        if (ids.Length == 0)
        {
            return Array.Empty<SimulatedShipment>();
        }

        var keys = ids.Select(id => (RedisKey)(ShipmentPrefix + id)).ToArray();
        var values = await _db.StringGetAsync(keys).WaitAsync(cancellationToken);

        var shipments = new List<SimulatedShipment>(values.Length);
        foreach (var value in values)
        {
            if (!value.IsNullOrEmpty)
            {
                var shipment = JsonSerializer.Deserialize<SimulatedShipment>((string)value!);
                if (shipment is not null)
                {
                    shipments.Add(shipment);
                }
            }
        }

        return shipments;
    }

    public async Task<SimulatedShipment?> FindByShipmentIdAsync(Guid shipmentId, CancellationToken cancellationToken = default)
        => await GetByIdAsync(shipmentId.ToString(), cancellationToken);

    public async Task<SimulatedShipment?> FindByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
        => (await GetAllAsync(cancellationToken)).FirstOrDefault(s => s.OrderId == orderId);
}

public class StatusHistoryEntry
{
    public string Status { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}

public class SimulatedShipment
{
    public Guid ShipmentId { get; set; }
    public Guid OrderId { get; set; }
    public string OriginAddress { get; set; } = string.Empty;
    public string DestinationAddress { get; set; } = string.Empty;
    public string Carrier { get; set; } = string.Empty;
    public string TrackingNumber { get; set; } = string.Empty;
    public string Status { get; set; } = "accepted";
    public string WebhookUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime EstimatedDelivery { get; set; }
    public DateTime? PickedUpAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string? IssueDetails { get; set; }
    public List<StatusHistoryEntry> StatusHistory { get; set; } = [new() { Status = "accepted" }];
}

