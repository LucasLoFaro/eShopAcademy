using MongoDB.Bson;

namespace Domain.Entities
{
    public class Location
    {
        public String Address { get; set; }
        public float Latitude { get; set; }
        public float Longitude { get; set; }        
    }
}
