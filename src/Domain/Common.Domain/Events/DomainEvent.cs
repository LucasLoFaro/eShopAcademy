namespace Common.Domain.Events;

public abstract record DomainEvent : BaseMessage
{
    public Guid OrderId { get; init; }
    public override Guid CorrelationId => OrderId;
    public virtual string EventType => GetType().Name.Replace("Event", "");
}