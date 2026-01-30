namespace Common.Domain.Events.Payments;

public record PaymentInitiatedEvent : PaymentEvent
{
    public string Provider { get; init; } = string.Empty;
    public string PaymentUrl { get; init; } = string.Empty;
}
