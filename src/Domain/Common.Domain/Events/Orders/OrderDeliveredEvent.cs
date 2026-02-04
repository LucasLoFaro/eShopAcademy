namespace Domain.Common.Events.Orders;

public record OrderDeliveredEvent : OrderEvent
{
    public string TrackingNumber { get; init; } = string.Empty;
    public DateTime DeliveredAt { get; init; } = DateTime.UtcNow;
}