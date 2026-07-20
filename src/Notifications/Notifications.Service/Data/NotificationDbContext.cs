using Domain.Notifications.Entities;
using MongoDB.Driver;

namespace NotificationService.Data;

public class NotificationDbContext
{
    private readonly IMongoDatabase _database;
    public IMongoClient Client { get; }
    public IMongoDatabase Database => _database;

    public NotificationDbContext(string connectionString, string databaseName)
    {
        Client = new MongoClient(connectionString);
        _database = Client.GetDatabase(databaseName);
    }

    public virtual IMongoCollection<NotificationMessage> Notifications =>
        _database.GetCollection<NotificationMessage>("Notifications");

    public virtual async Task PingAsync(CancellationToken cancellationToken)
        => await _database.RunCommandAsync<MongoDB.Bson.BsonDocument>(
            new MongoDB.Bson.BsonDocument("ping", 1),
            cancellationToken: cancellationToken);
}
