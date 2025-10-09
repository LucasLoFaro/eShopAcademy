namespace Domain.Events;

public class ShippingStartedEvent : BaseEvent
{
    public Guid ShippingId { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
}
