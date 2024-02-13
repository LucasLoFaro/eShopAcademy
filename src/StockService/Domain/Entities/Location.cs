using MongoDB.Bson;

namespace Domain.Entities
{
    public class Location
    {
        public String Address { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }        
    }
}
