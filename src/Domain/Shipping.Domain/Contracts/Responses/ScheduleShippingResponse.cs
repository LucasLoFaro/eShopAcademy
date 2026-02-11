namespace Domain.Shipping.Contracts.Responses;

public sealed record ScheduleShippingResponse(
    Guid ShipmentId,
    Guid OrderId,
    string Carrier,
    string TrackingNumber,
    string Status);
