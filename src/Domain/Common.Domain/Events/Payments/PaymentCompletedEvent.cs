namespace Domain.Common.Events.Payments;

public record PaymentCompletedEvent : PaymentEvent
{
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";
    public string PSPTransactionId { get; init; } = string.Empty;
}
