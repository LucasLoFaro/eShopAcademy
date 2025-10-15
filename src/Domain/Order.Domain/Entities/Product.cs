namespace Domain.Order.Entities;

public class Product
{
    public Guid ID { get; set; }
    public string Name { get; set; }
    public float Price { get; set; }
    public int Stock { get; set; }
    public string Description { get; set; }
    public String Image { get; set; }
    public String CategoryName { get; set; }
}
