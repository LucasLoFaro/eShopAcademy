namespace Common.Domain.Events.Stock;

public record StockReservationCreatedEvent : StockEvent
{
    public Guid ReservationId { get; init; }
}
