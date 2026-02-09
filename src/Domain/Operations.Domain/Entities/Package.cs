using Domain.Operations.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Domain.Operations.Entities;

public class Package : BaseEntity
{
    [BsonRepresentation(BsonType.String)]
    public Guid OrderId { get; set; }
    [BsonRepresentation(BsonType.String)]
    public Guid? ReservationId { get; set; }
    public PackageStatus Status { get; set; } = PackageStatus.Pending;
    public List<PackageItem> Items { get; set; } = new();
    public DateTime? PreparedAt { get; set; }
    public DateTime? ReadyAt { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string IssueType { get; set; } = string.Empty;
    public string IssueDetails { get; set; } = string.Empty;
    public string ReportedBy { get; set; } = string.Empty;
    public DateTime? IssueReportedAt { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
