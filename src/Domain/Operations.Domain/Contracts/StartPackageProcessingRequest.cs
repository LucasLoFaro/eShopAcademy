namespace Domain.Operations.Contracts;

public record StartPackageProcessingRequest
{
    public Guid ReservationId { get; init; }
    public string? CustomerName { get; init; }
    public string? CustomerEmail { get; init; }
}
