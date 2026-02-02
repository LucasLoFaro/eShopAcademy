namespace Common.Domain.Events.Orders;

public record OrderExpiredEvent : OrderEvent
{
    public DateTime ExpiredAt { get; init; } = DateTime.UtcNow;
}