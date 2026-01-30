namespace Common.Domain.Events.Payments;

public record PaymentRefundedEvent : PaymentEvent
{
    public decimal Amount { get; init; }
    public string Reason { get; init; } = string.Empty;
}