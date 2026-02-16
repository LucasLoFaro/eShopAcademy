using MongoDB.Bson.Serialization.Attributes;

namespace Domain.Customers.Entities;

public class SavedAddress : BaseEntity
{
    [BsonRepresentation(MongoDB.Bson.BsonType.String)]
    public Guid CustomerId { get; set; }
    public string Description { get; set; } = string.Empty; // e.g., "Home", "Work", "Office"
    public Address Address { get; set; } = new();
    public bool IsDefault { get; set; }
}

