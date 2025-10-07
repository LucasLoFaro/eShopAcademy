namespace Core.Domain.Events;

public class OrderSubmittedEvent : BaseEvent
{
    public Guid OrderId { get; set; }

    public OrderSubmittedEvent(Guid orderId)
    {
        OrderId = orderId;
    }
}