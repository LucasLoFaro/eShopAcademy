using System.ComponentModel.DataAnnotations;

namespace Core.Domain.Entities
{
    public class Product
    {
        public Guid ID { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public float Price { get; set; }
        public int Stock { get; set; }
        public string Description { get; set; }
        public String Image { get; set; }
        public String CategoryName { get; set; }
    }
}
