namespace Domain.Common.Events.Payments;

public record PaymentInitiatedEvent : PaymentEvent
{
    public string Provider { get; init; } = string.Empty;
    public string PaymentUrl { get; init; } = string.Empty;
}
