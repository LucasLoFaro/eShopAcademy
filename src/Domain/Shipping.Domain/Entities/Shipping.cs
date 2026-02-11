using Domain.Shipping.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Domain.Shipping.Entities;

public class Shipping : BaseEntity
{
    [BsonRepresentation(BsonType.String)]
    public Guid OrderId { get; set; }
    public Address Address { get; set; }
    public string Carrier { get; set; }
    public string TrackingNumber { get; set; }
    public ShippingStatus Status { get; set; } = ShippingStatus.Pending;
    public List<ShippingItem> Items { get; set; } = new();
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    
    // External tracking URL (e.g., https://tracking.dhl.com/ABC123)
    public string TrackingUrl
    {
        get
        {
            if (string.IsNullOrWhiteSpace(TrackingNumber) || string.IsNullOrWhiteSpace(Carrier))
                return string.Empty;
                
            return Carrier.ToLowerInvariant() switch
            {
                "dhl" => $"https://tracking.dhl.com/{TrackingNumber}",
                "fedex" => $"https://www.fedex.com/fedextrack/?tracknumbers={TrackingNumber}",
                "ups" => $"https://www.ups.com/track?tracknum={TrackingNumber}",
                "usps" => $"https://tools.usps.com/go/TrackConfirmAction?tLabels={TrackingNumber}",
                _ => string.Empty
            };
        }
    }
}
