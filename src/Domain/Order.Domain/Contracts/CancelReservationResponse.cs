namespace Domain.Order.Contracts;

public class CancelReservationResponse
{
    public Guid OrderId { get; init; }
    public Guid ReservationId { get; init; }
    public bool Success { get; init; }
    public string Reason { get; init; }
}
