using Domain.Shipping.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Domain.Shipping.Entities;

public class Shipping : BaseEntity
{
    [BsonRepresentation(BsonType.String)]
    public Guid OrderId { get; set; }
    public Address Address { get; set; }
    public string Carrier { get; set; }
    public string TrackingNumber { get; set; }
    public ShippingStatus Status { get; set; } = ShippingStatus.Pending;
    public List<ShippingItem> Items { get; set; } = new();
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
}
