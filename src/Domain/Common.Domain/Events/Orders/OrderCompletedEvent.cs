namespace Common.Domain.Events.Orders;

public record OrderCompletedEvent : OrderEvent
{
    public DateTime CompletedAt { get; init; } = DateTime.UtcNow;
}