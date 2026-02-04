namespace Domain.Common.Events.Payments;

public record PaymentFailedEvent : PaymentEvent
{
    public string Reason { get; init; } = string.Empty;
    public string? PSPTransactionId { get; init; }
}
