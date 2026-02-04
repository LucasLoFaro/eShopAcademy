namespace Domain.Shipping.Entities;

public class ShipmentItem
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}
