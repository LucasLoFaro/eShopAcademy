namespace Common.Domain.Commands.Stock;

public record ReleaseStockReservationCommand : StockCommand
{
    public string Reason { get; init; } = string.Empty;
}