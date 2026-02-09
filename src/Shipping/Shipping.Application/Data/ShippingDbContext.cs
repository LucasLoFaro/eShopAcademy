using Domain.Shipping.Entities;
using MongoDB.Driver;

namespace Shipping.Application.Data;

public sealed class ShippingDbContext
{
    private readonly IMongoDatabase _database;

    public ShippingDbContext(string connectionString, string databaseName)
    {
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(databaseName);
    }

    public IMongoCollection<ShippingStatusHistoryEntry> ShippingStatusHistory =>
        _database.GetCollection<ShippingStatusHistoryEntry>("ShippingStatusHistory");

    public IMongoCollection<ShippingInfo> ShippingInfos =>
        _database.GetCollection<ShippingInfo>("ShippingInfo");
}
