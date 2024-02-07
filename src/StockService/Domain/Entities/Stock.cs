using MongoDB.Bson;

namespace Domain.Entities
{
    public class Stock
    {
        public ObjectId _id { get; set; }
        
        //!! Puede pasar a dato tipo GUID
        public string ProductGuid { get; set; }
        public int Quantity { get; set; }

        //!! Puede pasar a tener su propia collection
        public string Warehouse { get; set; }
    }
}
