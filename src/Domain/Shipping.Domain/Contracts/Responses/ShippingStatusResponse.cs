namespace Domain.Shipping.Contracts.Responses;

public sealed record ShippingStatusResponse(
    Guid ShipmentId,
    Guid OrderId,
    string Status,
    string TrackingNumber,
    string Carrier,
    DateTime OccurredAt);
