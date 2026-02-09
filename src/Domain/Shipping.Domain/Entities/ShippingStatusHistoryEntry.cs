using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Domain.Shipping.Entities;

public sealed class ShippingStatusHistoryEntry
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [BsonRepresentation(BsonType.String)]
    public Guid ShipmentId { get; set; }

    [BsonRepresentation(BsonType.String)]
    public Guid OrderId { get; set; }

    public string Status { get; set; } = string.Empty;

    public string TrackingNumber { get; set; } = string.Empty;

    public string Carrier { get; set; } = string.Empty;

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
