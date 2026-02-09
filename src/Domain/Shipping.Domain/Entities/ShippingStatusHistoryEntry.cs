namespace Domain.Shipping.Entities;

public sealed class ShippingStatusHistoryEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ShipmentId { get; set; }

    public Guid OrderId { get; set; }

    public string Status { get; set; } = string.Empty;

    public string TrackingNumber { get; set; } = string.Empty;

    public string Carrier { get; set; } = string.Empty;

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
