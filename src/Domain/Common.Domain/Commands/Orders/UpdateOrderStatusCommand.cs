namespace Domain.Common.Commands.Orders;

public record UpdateOrderStatusCommand : OrderCommand
{
    public string Status { get; set; } = string.Empty;

    // Payment
    public Guid? PaymentId { get; set; }
    public string? ProviderTransactionId { get; set; }
    public decimal? Amount { get; set; }
    public string? PaymentStatus { get; set; }
    public DateTime? PaidAt { get; set; }

    // Shipping
    public string? ShippingStatus { get; set; }
    public string? DestinationAddress { get; set; }
    public string? TrackingNumber { get; set; }
    public string? Carrier { get; set; }
    public DateTime? ReadyForPickupAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }

    // Operations
    public string? OperatorName { get; set; }
    public DateTime? PackedAt { get; set; }

    // Stock
    public Guid? ReservationId { get; set; }
    public DateTime? StockCommittedAt { get; set; }
}
