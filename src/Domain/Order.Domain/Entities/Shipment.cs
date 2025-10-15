using Domain.Order.Enums;


namespace Domain.Order.Entities;

public class Shipment : BaseEntity
{
    public Guid OrderId { get; set; }
    public ShippingStatus Status { get; set; }
    public string TrackingNumber { get; set; }
    public string Carrier { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
}
