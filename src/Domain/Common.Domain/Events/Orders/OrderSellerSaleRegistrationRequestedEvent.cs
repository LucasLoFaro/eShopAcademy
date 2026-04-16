namespace Domain.Common.Events.Orders;

public record OrderSellerSaleRegistrationRequestedEvent : OrderEvent
{
    public Guid SellerId { get; init; }
    public Guid OrderItemId { get; init; }
    public decimal GrossAmount { get; init; }
    public decimal CommissionAmount { get; init; }
    public string Notes { get; init; } = string.Empty;
}
