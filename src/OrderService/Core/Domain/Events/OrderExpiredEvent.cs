namespace Core.Domain.Events;

public class OrderExpiredEvent : BaseEvent
{
    public Guid OrderId { get; set; }
    public Guid PaymentId { get; set; }
    public Guid ReservationId { get; set; }
}