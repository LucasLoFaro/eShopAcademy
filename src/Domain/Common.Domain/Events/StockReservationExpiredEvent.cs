namespace Domain.Common.Events;

public class StockReservationExpiredEvent : BaseEvent
{
    public Guid OrderId { get; init; }
    public Guid ReservationId { get; init; }
    public string Reason { get; init; } = "Order expired due to payment timeout.";
}
