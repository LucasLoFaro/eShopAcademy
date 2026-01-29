namespace Common.Domain.Events.Shipping;

public record ShippingStartedEvent : ShippingEvent
{
    public DateTime EstimatedDelivery { get; init; }
}
