namespace Domain.Shipping.Contracts.Requests;

public sealed record ShippingPickupConfirmationRequest
{
    public Guid ShippingId { get; init; }
    public Guid OrderId { get; init; }
    public DateTime ReadyAt { get; init; } = DateTime.UtcNow;
}
