using System;

namespace Domain
{
    public class Item
    {
        public Guid ProductID { get; set; }
        public int Quantity { get; set; }
        public double Price { get; set; }
    }
}