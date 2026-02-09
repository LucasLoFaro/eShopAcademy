namespace Domain.Common.Events.Operations;

public record PackageIssueReportedEvent : DomainEvent
{
    public string IssueType { get; init; } = string.Empty;
    public string Details { get; init; } = string.Empty;
    public string ReportedBy { get; init; } = string.Empty;
    public DateTime ReportedAt { get; init; } = DateTime.UtcNow;
    public Guid? ReservationId { get; init; }
}
