namespace Domain.Common.Events;

public class PaymentFailedEvent : BaseEvent
{
    public Guid OrderId { get; init; } = default!;
    public string PaymentSessionId { get; init; } = default!;
    public string Reason { get; init; } = string.Empty;
    public string PSPReference { get; init; } = string.Empty;
}
