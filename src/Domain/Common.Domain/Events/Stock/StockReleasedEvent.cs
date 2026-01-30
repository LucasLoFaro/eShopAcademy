namespace Common.Domain.Events.Stock;

public record StockReleasedEvent : StockEvent
{
    public Guid ReservationId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public DateTime ReleasedAt { get; init; } = DateTime.UtcNow;
}
