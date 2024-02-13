using MongoDB.Bson;

namespace Domain.Entities
{
    public class Stock
    {
        public ObjectId _id { get; set; }
        public Guid ProductID { get; set; }
        public int Quantity { get; set; }
        public Warehouse Warehouse { get; set; }
    }
}
