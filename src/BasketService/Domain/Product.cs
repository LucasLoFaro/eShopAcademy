using System;

namespace Domain
{
    public class Product
    {
        public Guid ID { get; set; }
        public String Name { get; set; }
        public double Price { get; set; }
        public int Stock { get; set; }
    }
}