namespace Domain.Common.Events.Orders;

public record OrderSubmittedEvent : OrderEvent
{
    public Guid PaymentId { get; set; }
    public Guid ReservationId { get; set; }
    public Guid CustomerId { get; init; }
    public Guid BasketClientId { get; init; }
    public decimal TotalAmount { get; init; }
    public string DestinationAddress { get; init; } = string.Empty;
}