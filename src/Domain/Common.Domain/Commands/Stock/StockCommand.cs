namespace Domain.Common.Commands.Stock;

public record StockCommand : BaseCommand
{
    public Guid ReservationId { get; init; }
}
