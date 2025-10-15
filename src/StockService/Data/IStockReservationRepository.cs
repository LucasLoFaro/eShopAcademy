using Domain.Stock.Entities;

namespace Infrastructure.Data;

public interface IStockReservationRepository
{
    Task CreateAsync(StockReservation reservation, CancellationToken ct);
    Task<StockReservation?> GetByIdAsync(Guid id, CancellationToken ct);
    Task UpdateAsync(StockReservation reservation, CancellationToken ct);
    Task<List<StockReservation>> GetExpiredAsync(CancellationToken ct);
}
