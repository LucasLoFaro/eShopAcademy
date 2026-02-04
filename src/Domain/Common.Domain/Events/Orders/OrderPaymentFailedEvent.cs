namespace Domain.Common.Events.Orders;

public record OrderPaymentFailedEvent : OrderEvent
{
    public Guid PaymentId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string? PSPTransactionId { get; init; }
    public DateTime FailedAt { get; init; } = DateTime.UtcNow;
}