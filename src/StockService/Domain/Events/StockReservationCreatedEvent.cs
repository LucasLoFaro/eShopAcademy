namespace Core.Domain.Events;

public class StockReservationCreatedEvent : BaseEvent
{
    public Guid OrderId { get; init; }
    public Guid ReservationId { get; init; }
}
