namespace Core.Domain.Events;

public class StockUpdatedEvent : BaseEvent
{
    public Guid ProductId { get; init; }
    public int Quantity { get; init; }
    public string WarehouseId { get; init; }
}
