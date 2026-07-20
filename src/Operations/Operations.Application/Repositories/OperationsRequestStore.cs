using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Microsoft.Extensions.Configuration;

namespace Operations.Application.Repositories;

public sealed class OperationsRequestStore
{
    private readonly IMongoCollection<OperationsRequest> _requests;

    public OperationsRequestStore(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("operations");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Missing connection string for the operations Mongo database.");

        var database = new MongoClient(connectionString)
            .GetDatabase(configuration["Operations:Database"] ?? "operations");
        _requests = database.GetCollection<OperationsRequest>("OperationsRequests");
    }

    public async Task<bool> TryBeginAsync(string requestId, CancellationToken cancellationToken)
    {
        try
        {
            await _requests.InsertOneAsync(
                new OperationsRequest { Id = requestId, StartedAt = DateTime.UtcNow },
                cancellationToken: cancellationToken);
            return true;
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
    }
}

public sealed class OperationsRequest
{
    [BsonId]
    public required string Id { get; init; }
    public DateTime StartedAt { get; init; }
}
