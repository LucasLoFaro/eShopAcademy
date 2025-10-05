using Core.Domain.Entities;


namespace Core.Domain.Events;

public class OrderSubmittedEvent : BaseEvent
{
    public Order Order { get; set; }

    public OrderSubmittedEvent(Order order)
    {
        Order = order;
    }
}