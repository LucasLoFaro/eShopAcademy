namespace Domain.Common.Events.Customers;

public abstract record CustomerEvent : BaseMessage
{
    public Guid CustomerId { get; set; }
    public override Guid CorrelationId => CustomerId;
    public virtual string EventType => GetType().Name.Replace("Event", "");
}
