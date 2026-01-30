namespace Common.Domain.Events.Orders;

public record OrderCancelledEvent : OrderEvent
{
    public string Reason { get; set; } = string.Empty;
}