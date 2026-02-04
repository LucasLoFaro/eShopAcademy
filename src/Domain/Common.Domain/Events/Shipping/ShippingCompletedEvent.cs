namespace Domain.Common.Events.Shipping;

public record ShippingCompletedEvent : ShippingEvent
{
    public DateTime DeliveredAt { get; init; } = DateTime.UtcNow;
}
