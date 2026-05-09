using Domain.Notification.Entities;
using MongoDB.Driver;

namespace NotificationService.Data;

public class NotificationDbContext
{
    private readonly IMongoDatabase _database;

    public NotificationDbContext(string connectionString, string databaseName)
    {
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(databaseName);
    }

    public virtual IMongoCollection<NotificationMessage> Notifications =>
        _database.GetCollection<NotificationMessage>("Notifications");
}
