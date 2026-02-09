using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Domain.Shipping.Entities;

public sealed class ShippingInfo
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [BsonRepresentation(BsonType.String)]
    public Guid OrderId { get; set; }

    public string CustomerEmail { get; set; } = string.Empty;

    public string? CustomerName { get; set; }
}
