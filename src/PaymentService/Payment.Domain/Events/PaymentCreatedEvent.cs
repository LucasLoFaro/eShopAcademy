namespace Domain.Events;

public class PaymentCreatedEvent : BaseEvent
{
    public string OrderId { get; init; } = default!;
    public string ProviderTransactionId { get; init; } = default!;
}
