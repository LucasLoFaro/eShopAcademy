using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;


namespace Core.Domain.Entities;

public class StockReservation
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; } = Guid.NewGuid();
    [BsonRepresentation(BsonType.String)]
    public Guid OrderId { get; set; }
    public List<ReservationItem> Items { get; set; } = new();
    public bool IsCommitted { get; set; } = false;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime ValidUntil { get; init; } = DateTime.UtcNow.AddMinutes(5);
    public DateTime? CommittedAt { get; set; }

    public StockReservation() { }
    public StockReservation(string orderId)
    {
        OrderId = Guid.Parse(orderId);
    }
}

public class ReservationItem
{
    public List<StockItem> Items { get; set; } = new();
    public string Warehouse { get; set; }
}

public class StockItem
{
    [BsonRepresentation(BsonType.String)]
    public Guid ProductID { get; set; }
    public int Quantity { get; set; }
}