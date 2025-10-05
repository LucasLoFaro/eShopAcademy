namespace Core.Domain.Entities;

public class Item
{
    public Guid ProductID { get; set; }
    public int Quantity { get; set; }
    public float Price { get; set; }
}
