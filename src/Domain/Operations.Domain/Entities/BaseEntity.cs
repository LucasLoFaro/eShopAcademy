using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Domain.Operations.Entities;

public class BaseEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
}