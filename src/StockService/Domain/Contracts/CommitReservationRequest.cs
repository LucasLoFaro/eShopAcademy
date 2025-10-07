namespace Core.Domain.Contracts;

public class CommitReservationRequest
{
    public Guid OrderId { get; init; }
    public Guid ReservationId { get; init; }
}
