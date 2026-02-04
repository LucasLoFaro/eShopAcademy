namespace Domain.Common.Events.Stock;

public record StockReservationCommittedEvent : StockEvent
{
    public Guid ReservationId { get; init; }
    public DateTime CommittedAt { get; init; } = DateTime.UtcNow;
}