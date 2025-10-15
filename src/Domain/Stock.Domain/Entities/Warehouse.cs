using MongoDB.Bson;

namespace Domain.Stock.Entities;

public class Warehouse
{
    public ObjectId _id { get; set; }
    public String Name { get; set; }
    public Location Location { get; set; }
}
