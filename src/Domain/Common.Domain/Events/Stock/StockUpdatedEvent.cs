namespace Common.Domain.Events.Stock;

public record StockUpdatedEvent : StockEvent
{
    public Guid ProductId { get; init; }
    public int Quantity { get; init; }
    public string WarehouseId { get; init; }
}
