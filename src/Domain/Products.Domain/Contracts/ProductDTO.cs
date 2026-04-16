namespace Domain.Products.Contracts;

public class ProductDTO
{
    public Guid ID { get; set; }
    public Guid SellerId { get; set; }
    public string Name { get; set; }
    public double Price { get; set; }
}
