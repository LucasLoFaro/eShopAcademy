using Core.Domain.Entities;


namespace Core.Domain.Contracts;

public class ReserveStockRequest
{
    public Guid OrderId { get; init; }
    public List<Stock> Stock { get; init; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
