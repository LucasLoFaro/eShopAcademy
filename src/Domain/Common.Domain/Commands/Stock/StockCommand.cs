namespace Common.Domain.Commands.Stock;

public record StockCommand : BaseCommand
{
    public Guid ReservationId { get; init; }
}
