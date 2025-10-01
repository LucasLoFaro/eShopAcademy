using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;


namespace Core.Domain.Entities
{
    public class Stock
    {
        [BsonId]
        public ObjectId _id { get; set; }

        [BsonRepresentation(BsonType.String)]
        public Guid ProductID { get; set; }
        public int Quantity { get; set; }
        public String Warehouse { get; set; }
        //public Warehouse Warehouse { get; set; }
    }
}
