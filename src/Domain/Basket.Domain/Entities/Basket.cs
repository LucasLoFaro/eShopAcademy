namespace Domain.Basket.Entities;

public class Basket
{
    public Guid ClientID { get; set; }
    public List<Item> Items { get; set; }
}
