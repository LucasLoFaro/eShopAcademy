namespace Common.Domain.Events.Orders;

public record OrderReadyForPickupEvent : OrderEvent
{
    public DateTime ReadyAt { get; init; } = DateTime.UtcNow;
}
