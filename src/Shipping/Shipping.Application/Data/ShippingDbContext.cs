using Domain.Shipping.Entities;
using MongoDB.Driver;

namespace Shipping.Application.Data;

public sealed class ShippingDbContext
{
    private readonly IMongoDatabase _database;
    public IMongoClient Client { get; }
    public IMongoDatabase Database => _database;

    public ShippingDbContext(string connectionString, string databaseName)
    {
        Client = new MongoClient(connectionString);
        _database = Client.GetDatabase(databaseName);
    }

    public IMongoCollection<ShippingStatusHistoryEntry> ShippingStatusHistory =>
        _database.GetCollection<ShippingStatusHistoryEntry>("ShippingStatusHistory");

    public IMongoCollection<ShippingInfo> ShippingInfos =>
        _database.GetCollection<ShippingInfo>("ShippingInfo");

    public async Task PingAsync(CancellationToken cancellationToken)
        => await _database.RunCommandAsync<MongoDB.Bson.BsonDocument>(
            new MongoDB.Bson.BsonDocument("ping", 1),
            cancellationToken: cancellationToken);
}
