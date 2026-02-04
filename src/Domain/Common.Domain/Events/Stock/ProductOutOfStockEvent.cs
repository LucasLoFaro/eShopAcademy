namespace Domain.Common.Events.Stock;

public record ProductOutOfStockEvent : StockEvent
{
    public Guid ProductId { get; init; }
}
