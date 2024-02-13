using System;

namespace Domain.Entities
{
    public class Product
    {
        public Guid ID { get; set; }
        public string Name { get; set; }
        public double Price { get; set; }
        public int? Stock { get; set; }
    }
}