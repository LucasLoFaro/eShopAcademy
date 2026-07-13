using System.Text.Json.Serialization;
using Domain.Operations.Entities;
using Domain.Operations.Enums;

namespace Domain.Operations.Contracts;

public record PackageItemResponse(Guid ProductId, string ProductName, int Quantity);

public record PackageResponse(
    Guid OrderId,
    Guid? ReservationId,
    string CustomerName,
    string CustomerEmail,
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    PackageStatus Status,
    DateTime? PreparedAt,
    DateTime? ReadyAt,
    string IssueType,
    string IssueDetails,
    string ReportedBy,
    DateTime? IssueReportedAt,
    DateTime UpdatedAt,
    List<PackageItemResponse> Items)
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
            package.UpdatedAt,
            package.Items.Select(i => new PackageItemResponse(i.ProductId, i.ProductName, i.Quantity)).ToList());
}
