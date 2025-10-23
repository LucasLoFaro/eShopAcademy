namespace Domain.Common.Events;

public class OrderCompletedEvent : OrderBaseEvent
{
    public Guid ShippingId { get; set; }
    public override string EventType => "Completed";
}