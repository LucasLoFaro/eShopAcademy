using Domain.Operations.Entities;
using Domain.Operations.Enums;

namespace Domain.Operations.Contracts;

public record PackageResponse(
    Guid OrderId,
    Guid? ReservationId,
    string CustomerName,
    string CustomerEmail,
    PackageStatus Status,
    DateTime? PreparedAt,
    DateTime? ReadyAt,
    string IssueType,
    string IssueDetails,
    string ReportedBy,
    DateTime? IssueReportedAt,
    DateTime UpdatedAt)
{
    public static PackageResponse FromPackage(Package package)
        => new(
            package.OrderId,
            package.ReservationId,
            package.CustomerName,
            package.CustomerEmail,
            package.Status,
            package.PreparedAt,
            package.ReadyAt,
            package.IssueType,
            package.IssueDetails,
            package.ReportedBy,
            package.IssueReportedAt,
            package.UpdatedAt);
}
