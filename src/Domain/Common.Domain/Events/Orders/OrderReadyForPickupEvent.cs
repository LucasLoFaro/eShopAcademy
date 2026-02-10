namespace Domain.Common.Events.Orders;

public record OrderReadyForPickupEvent : OrderEvent
{
    public DateTime ReadyAt { get; init; } = DateTime.UtcNow;
    public string OperatorName { get; init; } = string.Empty;
}
