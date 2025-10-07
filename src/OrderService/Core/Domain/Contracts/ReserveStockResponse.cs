namespace Domain.Contracts;

public class ReserveStockResponse
{
    public Guid? ReservationId { get; set; }
    public List<string> OutOfStockProducts { get; set; } = new();
    public bool Success { get => OutOfStockProducts.Count == 0; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime ValidUntil { get; init; } = DateTime.UtcNow.AddMinutes(5);
}
