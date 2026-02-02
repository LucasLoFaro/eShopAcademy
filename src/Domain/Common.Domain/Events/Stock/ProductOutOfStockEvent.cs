namespace Common.Domain.Events.Stock;

public record ProductOutOfStockEvent : StockEvent
{
    public Guid ProductId { get; init; }
}
