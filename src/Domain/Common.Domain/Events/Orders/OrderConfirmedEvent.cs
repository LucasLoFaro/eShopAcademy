namespace Domain.Common.Events.Orders;

public record OrderConfirmedEvent : OrderEvent
{
    public Guid PaymentId { get; set; }
    public decimal Amount { get; init; }
    public DateTime ConfirmedAt { get; init; } = DateTime.UtcNow;
}