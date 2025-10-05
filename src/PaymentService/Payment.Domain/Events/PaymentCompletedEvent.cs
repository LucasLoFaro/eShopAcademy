namespace Domain.Events;

public class PaymentCompletedEvent : BaseEvent
{
    public Guid OrderId { get; init; }
    public Guid PaymentSessionId { get; init; }
    public string PSPReference { get; init; } = string.Empty;
}
