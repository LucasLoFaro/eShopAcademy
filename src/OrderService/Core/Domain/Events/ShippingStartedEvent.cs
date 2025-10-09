namespace Core.Domain.Events;

public class ShippingStartedEvent : BaseEvent
{
    public Guid OrderId { get; init; }
    public Guid ShipmentId { get; init; }
    public string TrackingNumber { get; init; } = string.Empty;
    public DateTime EstimatedDelivery { get; init; }
}
