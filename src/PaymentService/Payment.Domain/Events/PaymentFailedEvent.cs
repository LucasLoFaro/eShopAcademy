namespace Domain.Events;

public class PaymentFailedEvent : BaseEvent
{
    public Guid OrderId { get; init; }
    public Guid PaymentSessionId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string PSPReference { get; init; } = string.Empty;
}
