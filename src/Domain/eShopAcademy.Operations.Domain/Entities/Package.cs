using Domain.Operations.Enums;

namespace Domain.Operations.Entities;

public class Package : BaseEntity
{
    public Guid OrderId { get; set; }
    public Guid ReservationId { get; set; }
    public PackageStatus Status { get; set; } = PackageStatus.Pending;
    public List<PackageItem> Items { get; set; } = new();
    public DateTime? PreparedAt { get; set; }
}
