using System.ComponentModel.DataAnnotations;

namespace Core.Domain.Entities
{
    public class Product
    {
        public Guid ProductId { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public decimal Price { get; set; }
        public string Image { get; set; }
    }
}
