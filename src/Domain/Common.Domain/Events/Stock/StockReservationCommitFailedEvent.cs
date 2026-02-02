namespace Common.Domain.Events.Stock;

public record StockReservationCommitFailedEvent : StockEvent
{
    public Guid ReservationId { get; init; }
    public string Reason { get; init; } = string.Empty;
}
