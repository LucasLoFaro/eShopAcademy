using Core.Domain.Enums;


namespace Core.Domain.Entities;

public class Order : BaseEntity
{
    public Customer Customer { get; set; }
    public Guid CustomerId { get; set; }
    public Payment Payment { get; set; }
    public Guid PaymentId { get; set; }
    public List<OrderItem> Items { get; set; } = new();
    public double Total { get; set; }
    public OrderStatus Status { get; set; }
    public BillingStatus BillingStatus { get; set; }
    public ShippingStatus ShippingStatus { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    
}
