namespace Domain.Common.Events.Shipping;

public record ShippingConfirmedEvent : ShippingEvent
{
    public DateTime EstimatedDelivery { get; init; }
}
