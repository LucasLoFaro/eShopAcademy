using System.ComponentModel.DataAnnotations;

namespace Domain.Product.Entities;

public class Category
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string Name { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
}