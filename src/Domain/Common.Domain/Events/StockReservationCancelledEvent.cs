namespace Domain.Common.Events;

public class StockReservationCancelledEvent : BaseEvent
{
    public Guid OrderId { get; init; }
    public Guid ReservationId { get; init; }
    public string Reason { get; init; } = string.Empty;
}
