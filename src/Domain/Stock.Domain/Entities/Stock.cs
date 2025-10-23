using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;


namespace Domain.Stock.Entities;

public class Stock
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid ProductID { get; set; }
    public int Quantity { get; set; }
    public String Warehouse { get; set; }
    //public Warehouse Warehouse { get; set; }
}
