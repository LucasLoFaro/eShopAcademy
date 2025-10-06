namespace Domain.Events;

public class PaymentCompletedEvent : BaseEvent
{
    public string OrderId { get; init; } = default!;
    public string PaymentSessionId { get; init; } = default!;
    public string PSPReference { get; init; } = string.Empty;
}
