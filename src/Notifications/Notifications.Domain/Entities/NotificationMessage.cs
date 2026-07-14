using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Domain.Notifications.Enums;

namespace Domain.Notifications.Entities;

public class NotificationMessage : BaseEntity
{
    public NotificationRecipient Recipient { get; set; }
    public NotificationChannel Channel { get; set; }
    public string Subject { get; set; }
    public string Body { get; set; }
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    public DateTime? SentAt { get; set; }
    public DateTime? FailedAt { get; set; }
    public string? Error { get; set; }

    [BsonRepresentation(BsonType.String)]
    public Guid OrderId { get; set; }
    public string Type { get; set; } = string.Empty;
    public bool IsRead { get; set; }
}
