using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Domain.Operations.Entities;

public class PackageItem
{
    [BsonRepresentation(BsonType.String)]
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
}
