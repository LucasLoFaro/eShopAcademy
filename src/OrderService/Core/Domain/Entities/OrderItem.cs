namespace Core.Domain.Entities;

public class OrderItem : BaseEntity
{
    public Guid OrderId { get; set; }
    public Guid ProductID { get; set; }
    public Product Product { get; set; }    
    public int Quantity { get; set; }
    public double Price => Product.Price * Quantity;
}