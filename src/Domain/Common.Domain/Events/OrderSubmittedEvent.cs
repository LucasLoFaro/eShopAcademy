namespace Domain.Common.Events;

public class OrderSubmittedEvent : OrderBaseEvent
{
    public Guid PaymentId { get; set; }
    public Guid ReservationId { get; set; }
    public override string EventType => "Submitted";
}