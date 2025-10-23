namespace Domain.Common.Events;

public class OrderFailedEvent : OrderBaseEvent
{
    public Guid PaymentId { get; set; }
    public Guid ReservationId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public override string EventType => "Failed";
}