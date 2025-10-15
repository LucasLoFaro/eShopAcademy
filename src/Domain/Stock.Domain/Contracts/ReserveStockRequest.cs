namespace Domain.Stock.Contracts;

public class ReserveStockRequest
{
    public Guid OrderId { get; init; }
    public List<Entities.Stock> Stock { get; init; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
