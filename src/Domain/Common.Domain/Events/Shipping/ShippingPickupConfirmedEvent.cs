namespace Domain.Common.Events.Shipping;

public record ShippingPickupConfirmedEvent : ShippingEvent
{
    public DateTime EstimatedDelivery { get; init; }
}
