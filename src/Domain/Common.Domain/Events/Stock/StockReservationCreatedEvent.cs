namespace Domain.Common.Events.Stock;

public record StockReservationCreatedEvent : StockEvent
{
    public Guid ReservationId { get; init; }
}
