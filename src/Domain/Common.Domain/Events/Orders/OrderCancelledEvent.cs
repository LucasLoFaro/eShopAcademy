namespace Domain.Common.Events.Orders;

public record OrderCancelledEvent : OrderEvent
{
    public string Reason { get; init; } = string.Empty;
    public DateTime CancelledAt { get; init; } = DateTime.UtcNow;
}