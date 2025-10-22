namespace Domain.Orders.Enums;

public enum OrderStatus
{
    Created,
    Paid,
    Confirmed,
    Processing,
    ReadyForPickup,
    Shipped,
    Delivered,
    Cancelled,
    Error
}
