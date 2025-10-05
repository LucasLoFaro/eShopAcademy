namespace Core.Domain.Contracts;

public class StockReservationResponse
{
    public Guid? ReservationId { get; set; }
    public List<string> OutOfStockProducts { get; set; } = new();
    public bool Success { get => OutOfStockProducts.Any(); }
}
