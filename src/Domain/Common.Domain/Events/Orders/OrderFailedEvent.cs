namespace Common.Domain.Events.Orders;

public record OrderFailedEvent : OrderEvent
{
    public string Stage { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public DateTime FailedAt { get; init; } = DateTime.UtcNow;
}