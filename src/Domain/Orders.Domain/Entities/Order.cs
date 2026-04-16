using Domain.Orders.Enums;


namespace Domain.Orders.Entities;

public class Order : BaseEntity
{
    // Order lifecycle
    public OrderStatus Status { get; set; }
    public double TotalPrice { get; set; }

    // Customer
    public Guid CustomerId { get; set; }
    public OrderCustomerInfo Customer { get; set; } = new();

    // Items
    public List<OrderItem> Items { get; set; } = new();

    // Domain sub-objects
    public OrderPaymentInfo Payment { get; set; } = new();
    public OrderShippingInfo Shipping { get; set; } = new();
    public OrderStockReservationInfo Stock { get; set; } = new();
    public OrderOperationsInfo Operations { get; set; } = new();
    public OrderBillingInfo Billing { get; set; } = new();
    public DateTime? SellerSalesRegisteredAt { get; set; }
}
