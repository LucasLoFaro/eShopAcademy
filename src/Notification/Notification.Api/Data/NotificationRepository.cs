using Domain.Notification.Entities;
using MongoDB.Driver;

namespace Notification.Api.Data;

public class NotificationRepository : INotificationRepository
{
    private readonly IMongoCollection<NotificationMessage> _notifications;

    public NotificationRepository(NotificationDbContext context)
    {
        _notifications = context.Notifications;
    }

    public async Task<IReadOnlyList<NotificationMessage>> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var filter = Builders<NotificationMessage>.Filter.Eq(n => n.Recipient.Email, email);
        var sort = Builders<NotificationMessage>.Sort.Descending(n => n.CreatedAt);
        return await _notifications.Find(filter).Sort(sort).ToListAsync(ct);
    }

    public async Task<bool> MarkAsReadAsync(Guid id, CancellationToken ct = default)
    {
        var filter = Builders<NotificationMessage>.Filter.Eq(n => n.Id, id);
        var update = Builders<NotificationMessage>.Update
            .Set(n => n.IsRead, true)
            .Set(n => n.ModifiedAt, DateTime.UtcNow);

        var result = await _notifications.UpdateOneAsync(filter, update, cancellationToken: ct);
        return result.ModifiedCount > 0;
    }

    public async Task<long> MarkAllAsReadAsync(string email, CancellationToken ct = default)
    {
        var filter = Builders<NotificationMessage>.Filter.And(
            Builders<NotificationMessage>.Filter.Eq(n => n.Recipient.Email, email),
            Builders<NotificationMessage>.Filter.Eq(n => n.IsRead, false));

        var update = Builders<NotificationMessage>.Update
            .Set(n => n.IsRead, true)
            .Set(n => n.ModifiedAt, DateTime.UtcNow);

        var result = await _notifications.UpdateManyAsync(filter, update, cancellationToken: ct);
        return result.ModifiedCount;
    }
}
