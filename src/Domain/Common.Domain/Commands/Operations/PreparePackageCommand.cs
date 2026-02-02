namespace Common.Domain.Commands.Operations;

public record PreparePackageCommand : OperationsCommand
{
    public Guid ReservationId { get; init; }
}