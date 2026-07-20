using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Infrastructure.Data;

public interface IStockOperationStore
{
    Task<bool> TryBeginAsync(string operationId, CancellationToken cancellationToken);
}

public sealed class StockOperationStore : IStockOperationStore
{
    private readonly IMongoCollection<StockOperation> _operations;

    public StockOperationStore(StockDbContext context)
    {
        _operations = context.Database.GetCollection<StockOperation>("StockOperations");
    }

    public async Task<bool> TryBeginAsync(string operationId, CancellationToken cancellationToken)
    {
        try
        {
            await _operations.InsertOneAsync(
                new StockOperation { Id = operationId, StartedAt = DateTime.UtcNow },
                cancellationToken: cancellationToken);
            return true;
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
    }
}

public sealed class StockOperation
{
    [BsonId]
    public required string Id { get; init; }
    public DateTime StartedAt { get; init; }
}
