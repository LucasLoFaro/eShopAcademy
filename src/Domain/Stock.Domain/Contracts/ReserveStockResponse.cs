using Domain.Stock.Entities;

namespace Domain.Stock.Contracts;

public class ReserveStockResponse
{
    public Guid? ReservationId { get; set; }
    public List<string> OutOfStockProducts { get; set; } = new();
    public bool Success { get => OutOfStockProducts.Any(); }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime ValidUntil { get; init; } = DateTime.UtcNow.AddMinutes(5);

    public ReserveStockResponse(StockReservation reservation)
    {
        ReservationId = reservation.Id;
    }
}
