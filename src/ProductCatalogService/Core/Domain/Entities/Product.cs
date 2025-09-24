using System.ComponentModel.DataAnnotations;

namespace Core.Domain.Entities;

public class Product
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string Name { get; set; }

    [Required]
    public double Price { get; set; }

    public string Description { get; set; }

    public string ImageUrl { get; set; }

    public string CategoryId { get; set; }
    public Category? Category { get; set; }
}
