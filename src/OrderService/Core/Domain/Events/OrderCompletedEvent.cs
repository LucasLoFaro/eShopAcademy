namespace Core.Domain.Events;

public class OrderCompletedEvent : BaseEvent
{
    public Guid OrderId { get; set; }
    public Guid ShippingId { get; set; }
}