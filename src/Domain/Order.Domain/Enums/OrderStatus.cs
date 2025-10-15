namespace Domain.Order.Enums;

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
