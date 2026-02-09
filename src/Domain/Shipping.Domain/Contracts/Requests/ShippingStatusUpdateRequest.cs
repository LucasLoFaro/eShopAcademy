namespace Domain.Shipping.Contracts.Requests;

public sealed record ShippingStatusUpdateRequest
{
    public Guid ShippingId { get; init; }

    public Guid OrderId { get; init; }

    public string Status { get; init; } = string.Empty;

    public string TrackingNumber { get; init; } = string.Empty;

    public string Carrier { get; init; } = string.Empty;

    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
