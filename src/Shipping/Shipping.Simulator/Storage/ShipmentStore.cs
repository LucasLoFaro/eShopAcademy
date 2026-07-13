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

    public async Task SaveAsync(SimulatedShipment shipment)
    {
        var json = JsonSerializer.Serialize(shipment);
        await _db.StringSetAsync(ShipmentPrefix + shipment.ShipmentId, json);
        await _db.SetAddAsync(ShipmentIndexKey, shipment.ShipmentId.ToString());
    }

    public async Task<SimulatedShipment?> GetByIdAsync(string id)
    {
        var json = await _db.StringGetAsync(ShipmentPrefix + id);
        return json.IsNullOrEmpty ? null : JsonSerializer.Deserialize<SimulatedShipment>((string)json!);
    }

    public async Task<IReadOnlyList<SimulatedShipment>> GetAllAsync()
    {
        var ids = await _db.SetMembersAsync(ShipmentIndexKey);
        if (ids.Length == 0)
        {
            return Array.Empty<SimulatedShipment>();
        }

        var keys = ids.Select(id => (RedisKey)(ShipmentPrefix + id)).ToArray();
        var values = await _db.StringGetAsync(keys);

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

    public async Task<SimulatedShipment?> FindByShipmentIdAsync(Guid shipmentId)
        => (await GetAllAsync()).FirstOrDefault(s => s.ShipmentId == shipmentId);

    public async Task<SimulatedShipment?> FindByOrderIdAsync(Guid orderId)
        => (await GetAllAsync()).FirstOrDefault(s => s.OrderId == orderId);
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

