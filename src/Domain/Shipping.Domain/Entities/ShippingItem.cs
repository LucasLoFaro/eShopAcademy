using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Domain.Shipping.Entities;

public class ShippingItem
{
    [BsonRepresentation(BsonType.String)]
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}
