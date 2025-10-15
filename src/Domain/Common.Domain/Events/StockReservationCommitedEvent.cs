namespace Domain.Common.Events;

public class StockReservationCommitedEvent : BaseEvent
{
    public Guid OrderId { get; init; }
    public Guid ReservationId { get; init; }
}
