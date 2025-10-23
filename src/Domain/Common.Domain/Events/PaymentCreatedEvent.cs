namespace Domain.Common.Events;

public class PaymentCreatedEvent : BaseEvent
{
    public Guid OrderId { get; init; } = default!;
    public string ProviderTransactionId { get; init; } = default!;
}
