namespace Domain.Order.Enums;

public enum ShippingStatus
{
    AwaitingConfirmation,
    Confirmed,
    ReadyForPickup,
    Shipped,
    Delivered,
    Returned
}
