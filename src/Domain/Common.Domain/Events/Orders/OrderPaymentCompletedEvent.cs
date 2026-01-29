namespace Common.Domain.Events.Orders;

public record OrderPaymentCompletedEvent : OrderEvent
{
    public Guid PaymentId { get; init; }
    public decimal Amount { get; init; }
    public string PSPTransactionId { get; init; } = string.Empty;
    public DateTime CompletedAt { get; init; } = DateTime.UtcNow;
}