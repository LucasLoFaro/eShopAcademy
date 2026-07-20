using Domain.Products.Entities;
using MongoDB.Driver;
using MongoDB.Bson;

namespace Infrastructure.Data;

public class ProductDbContext
{
    private readonly IMongoDatabase _database;

    public ProductDbContext(string connectionString, string databaseName)
    {
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(databaseName);
    }

    public IMongoCollection<Product> Products => _database.GetCollection<Product>("Products");

    public Task PingAsync(CancellationToken cancellationToken = default) =>
        _database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: cancellationToken);
}
