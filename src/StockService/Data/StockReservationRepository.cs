using Domain.Stock.Entities;
using MongoDB.Driver;


namespace Infrastructure.Data;
public class StockReservationRepository : IStockReservationRepository
{
    private readonly IMongoCollection<StockReservation> _reservations;

    public StockReservationRepository(StockDbContext context)
    {
        _reservations = context.Reservations;
    }

    public async Task CreateAsync(StockReservation reservation, CancellationToken ct)
        => await _reservations.InsertOneAsync(reservation, cancellationToken: ct);

    public async Task<StockReservation?> GetByIdAsync(Guid id, CancellationToken ct)
        => await _reservations.Find(x => x.Id == id).FirstOrDefaultAsync(ct);

    public async Task UpdateAsync(StockReservation reservation, CancellationToken ct)
        => await _reservations.ReplaceOneAsync(x => x.Id == reservation.Id, reservation, cancellationToken: ct);

    public async Task<List<StockReservation>> GetExpiredAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        return await _reservations.Find(x => !x.IsCommitted && x.ValidUntil < now).ToListAsync(ct);
    }
}
