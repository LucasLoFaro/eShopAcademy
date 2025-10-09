namespace Core.Domain.Events;

public class OrderFailedEvent : BaseEvent
{
    public Guid OrderId { get; set; }
    public Guid PaymentId { get; set; }
    public Guid ReservationId { get; set; }
    public string Reason { get; set; } = string.Empty;
}