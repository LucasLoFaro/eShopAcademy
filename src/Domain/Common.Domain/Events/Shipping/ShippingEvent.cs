namespace Domain.Common.Events.Shipping;

public abstract record ShippingEvent : DomainEvent
{
    public Guid ShipmentId { get; init; }
    public string Carrier { get; init; } = string.Empty;
    public string TrackingNumber { get; init; } = string.Empty;
    public string DestinationAddress { get; init; } = string.Empty;
}