namespace Domain.Common.Events;

public class PaymentCompletedEvent : BaseEvent
{
    public Guid OrderId { get; init; } = default!;
    public string ProviderTransactionId { get; init; } = default!;
}
