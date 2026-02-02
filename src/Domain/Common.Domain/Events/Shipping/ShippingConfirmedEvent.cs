namespace Common.Domain.Events.Shipping;

public record ShippingConfirmedEvent : ShippingEvent
{
    public DateTime EstimatedDelivery { get; init; }
}
