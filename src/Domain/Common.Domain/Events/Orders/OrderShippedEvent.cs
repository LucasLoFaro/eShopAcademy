namespace Common.Domain.Events.Orders;

public record OrderShippedEvent : OrderEvent
{
    public string TrackingNumber { get; init; } = string.Empty;
    public string Carrier { get; init; } = string.Empty;
    public DateTime ShippedAt { get; init; } = DateTime.UtcNow;
}
