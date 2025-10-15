namespace Domain.Order.Contracts;

public class CommitReservationResponse
{
    public Guid OrderId { get; init; }
    public Guid ReservationId { get; init; }
    public bool Success { get; init; }
}
