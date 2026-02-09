using Domain.Shipping.Entities;
using MongoDB.Driver;

namespace Shipping.Application.Data;

public interface IShippingInfoRepository
{
    Task UpsertAsync(ShippingInfo info, CancellationToken cancellationToken = default);
    Task<ShippingInfo?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
}

public sealed class ShippingInfoRepository : IShippingInfoRepository
{
    private readonly IMongoCollection<ShippingInfo> _collection;

    public ShippingInfoRepository(ShippingDbContext context)
    {
        _collection = context.ShippingInfos;
    }

    public Task UpsertAsync(ShippingInfo info, CancellationToken cancellationToken = default)
    {
        var filter = Builders<ShippingInfo>.Filter.Eq(e => e.OrderId, info.OrderId);
        return _collection.ReplaceOneAsync(filter, info, new ReplaceOptions { IsUpsert = true }, cancellationToken);
    }

    public async Task<ShippingInfo?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<ShippingInfo>.Filter.Eq(e => e.OrderId, orderId);
        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }
}
