namespace Domain.Common.Events.Stock;

public record StockUpdatedEvent : StockEvent
{
    public Guid ProductId { get; init; }
    public int Quantity { get; init; }
    public string WarehouseId { get; init; }
}
