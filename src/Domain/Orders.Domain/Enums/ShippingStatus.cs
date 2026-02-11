namespace Domain.Orders.Enums;

public enum ShippingStatus
{
    AwaitingConfirmation,
    Scheduled,
    ReadyForPickup,
    Shipped,
    Delivered,
    Returned
}
