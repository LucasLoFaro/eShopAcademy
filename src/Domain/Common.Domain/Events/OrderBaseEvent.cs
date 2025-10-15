namespace Domain.Common.Events;

public abstract class OrderBaseEvent : BaseEvent
{
    public Guid OrderId { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public abstract string EventType { get; }
}