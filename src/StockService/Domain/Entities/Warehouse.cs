using MongoDB.Bson;

namespace Domain.Entities
{
    public class Warehouse
    {
        public ObjectId _id { get; set; }
        
        public Guid ID { get; set; }
        public String Name { get; set; }
        public Location Location { get; set; }
    }
}
