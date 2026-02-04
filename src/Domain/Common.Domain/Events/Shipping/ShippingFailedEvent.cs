namespace Domain.Common.Events.Shipping;

public record ShippingFailedEvent : ShippingEvent
{
    public string Reason { get; init; } = string.Empty;
    public DateTime FailedAt { get; init; } = DateTime.UtcNow;
}
