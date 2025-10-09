namespace Core.Domain.Events;

public class ShippingCompletedEvent : BaseEvent
{
    public Guid OrderId { get; init; }
    public Guid ShippingId { get; init; }
    public string TrackingNumber { get; init; } = string.Empty;
    public DateTime DeliveredAt { get; init; }
}
