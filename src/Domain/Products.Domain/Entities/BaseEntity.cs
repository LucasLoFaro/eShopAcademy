using System.ComponentModel.DataAnnotations;

namespace Domain.Product.Entities;

public class BaseEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
}