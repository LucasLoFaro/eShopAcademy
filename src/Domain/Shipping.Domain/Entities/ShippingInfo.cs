namespace Domain.Shipping.Entities;

public sealed class ShippingInfo
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OrderId { get; set; }

    public string CustomerEmail { get; set; } = string.Empty;

    public string? CustomerName { get; set; }
}
