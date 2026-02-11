namespace Domain.Common.Events.Orders;

public record OrderStatusUpdatedEvent : OrderEvent
{
    public string Status { get; init; } = string.Empty;
    public decimal? Amount { get; init; }
    public string? Currency { get; init; }
    public string? TrackingNumber { get; init; }
    public string? Carrier { get; init; }
    public string? Reason { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
