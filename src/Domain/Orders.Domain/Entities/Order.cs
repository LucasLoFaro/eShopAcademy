using Domain.Orders.Enums;


namespace Domain.Orders.Entities;

public class Order : BaseEntity
{
    public Customer Customer { get; set; }
    public Guid CustomerId { get; set; }
    public Payment Payment { get; set; }
    public Guid PaymentId { get; set; }
    public Guid ReservationId { get; set; }
    public List<OrderItem> Items { get; set; } = new();
    public double TotalPrice { get; set; }
    public OrderStatus Status { get; set; }
    public BillingStatus BillingStatus { get; set; }
    public ShippingStatus ShippingStatus { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
}