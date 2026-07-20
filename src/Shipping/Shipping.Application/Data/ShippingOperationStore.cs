using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Shipping.Application.Data;

public interface IShippingOperationStore
{
    Task<bool> TryBeginAsync(Guid orderId, CancellationToken cancellationToken);
    Task AbandonAsync(Guid orderId, CancellationToken cancellationToken);
}

public sealed class ShippingOperationStore : IShippingOperationStore
{
    private readonly IMongoCollection<ShippingOperation> _operations;

    public ShippingOperationStore(ShippingDbContext context)
    {
        _operations = context.Database.GetCollection<ShippingOperation>("ShippingOperations");
    }

    public async Task<bool> TryBeginAsync(Guid orderId, CancellationToken cancellationToken)
    {
        try
        {
            await _operations.InsertOneAsync(
                new ShippingOperation { OrderId = orderId, StartedAt = DateTime.UtcNow },
                cancellationToken: cancellationToken);
            return true;
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
    }

    public Task AbandonAsync(Guid orderId, CancellationToken cancellationToken) =>
        _operations.DeleteOneAsync(operation => operation.OrderId == orderId, cancellationToken);
}

public sealed class ShippingOperation
{
    [BsonId]
    public Guid OrderId { get; init; }
    public DateTime StartedAt { get; init; }
}
