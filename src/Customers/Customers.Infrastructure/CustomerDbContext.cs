using Domain.Customers.Entities;
using MongoDB.Driver;

namespace Customers.Infrastructure.Data;

public class CustomerDbContext
{
    private readonly IMongoDatabase _database;

    public CustomerDbContext(string connectionString, string databaseName)
    {
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(databaseName);
    }

    public IMongoCollection<Customer> Customers => _database.GetCollection<Customer>("Customers");
}
