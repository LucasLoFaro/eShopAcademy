using Domain.Stock.Entities;
using MongoDB.Driver;


namespace Infrastructure.Data;

public class StockDbContext
{
    private readonly IMongoDatabase _database;
    public IMongoClient Client { get; }
    public IMongoDatabase Database => _database;

    public StockDbContext(string connectionString, string databaseName)
    {
        Client = new MongoClient(connectionString);
        _database = Client.GetDatabase(databaseName);
    }

    public IMongoCollection<Stock> Stocks => _database.GetCollection<Stock>("Stocks");
    public IMongoCollection<StockReservation> Reservations => _database.GetCollection<StockReservation>("Reservations");

    public async Task PingAsync(CancellationToken cancellationToken)
        => await _database.RunCommandAsync<MongoDB.Bson.BsonDocument>(
            new MongoDB.Bson.BsonDocument("ping", 1),
            cancellationToken: cancellationToken);
}
