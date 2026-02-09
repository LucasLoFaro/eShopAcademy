using Domain.Shipping.Entities;
using MongoDB.Driver;

namespace Shipping.Application.Data;

public interface IShippingStatusHistoryRepository
{
    Task AddAsync(ShippingStatusHistoryEntry entry, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShippingStatusHistoryEntry>> GetHistoryAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<ShippingStatusHistoryEntry?> GetLatestStatusAsync(Guid orderId, CancellationToken cancellationToken = default);
}

public sealed class ShippingStatusHistoryRepository : IShippingStatusHistoryRepository
{
    private readonly IMongoCollection<ShippingStatusHistoryEntry> _history;

    public ShippingStatusHistoryRepository(ShippingDbContext context)
    {
        _history = context.ShippingStatusHistory;
    }

    public Task AddAsync(ShippingStatusHistoryEntry entry, CancellationToken cancellationToken = default)
    {
        return _history.InsertOneAsync(entry, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<ShippingStatusHistoryEntry>> GetHistoryAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<ShippingStatusHistoryEntry>.Filter.Eq(e => e.OrderId, orderId);
        return await _history.Find(filter)
            .SortBy(e => e.OccurredAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<ShippingStatusHistoryEntry?> GetLatestStatusAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<ShippingStatusHistoryEntry>.Filter.Eq(e => e.OrderId, orderId);
        return await _history.Find(filter)
            .SortByDescending(e => e.OccurredAt)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
