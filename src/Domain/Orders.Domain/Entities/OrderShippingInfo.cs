using Domain.Orders.Enums;

namespace Domain.Orders.Entities;

public class OrderShippingInfo
{
    public ShippingStatus Status { get; set; }
    public string DestinationAddress { get; set; } = string.Empty;
    public string TrackingNumber { get; set; } = string.Empty;
    public string Carrier { get; set; } = string.Empty;
    public DateTime? ReadyForPickupAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    
    // Compensation tracking
    public DateTime? ReturnedAt { get; set; }
    
    // External tracking URL (e.g., https://tracking.dhl.com/ABC123)
    public string TrackingUrl { get; set; } = string.Empty;
}
