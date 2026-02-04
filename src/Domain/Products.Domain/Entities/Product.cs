using Domain.Product.Entities;
using System.ComponentModel.DataAnnotations;

namespace Domain.Products.Entities;

public class Product : BaseEntity
{
    [Required]
    public string Name { get; set; }

    [Required]
    public double Price { get; set; }

    public string Description { get; set; }

    public string ImageUrl { get; set; }

    public string CategoryId { get; set; }
    public Category? Category { get; set; }
}
