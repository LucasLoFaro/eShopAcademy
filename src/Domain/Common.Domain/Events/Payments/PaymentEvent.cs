namespace Common.Domain.Events.Payments;

public abstract record PaymentEvent : DomainEvent
{
    public Guid PaymentId { get; init; } = default!;
    public string ProviderTransactionId { get; init; } = default!;
}