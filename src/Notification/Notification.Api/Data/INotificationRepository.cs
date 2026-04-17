using Domain.Notification.Entities;

namespace Notification.Api.Data;

public interface INotificationRepository
{
    Task<IReadOnlyList<NotificationMessage>> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> MarkAsReadAsync(Guid id, CancellationToken ct = default);
    Task<long> MarkAllAsReadAsync(string email, CancellationToken ct = default);
}
