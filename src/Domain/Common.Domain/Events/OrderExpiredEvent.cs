namespace Domain.Common.Events;

public class OrderExpiredEvent : OrderBaseEvent
{
    public Guid PaymentId { get; set; }
    public Guid ReservationId { get; set; }
    public override string EventType => "Expired";
}