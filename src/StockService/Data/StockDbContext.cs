using Core.Domain.Entities;
using MongoDB.Driver;


namespace Infrastructure.Data;

public class StockDbContext
{
    private readonly IMongoDatabase _database;

    public StockDbContext(string connectionString, string databaseName)
    {
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(databaseName);
    }

    public IMongoCollection<Stock> Stocks => _database.GetCollection<Stock>("Stocks");
}