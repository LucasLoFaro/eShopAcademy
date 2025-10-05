namespace Core.Domain.Enums;

public enum ShippingStatus
{
    AwaitingConfirmation,
    Confirmed,
    ReadyForPickup,
    Shipped,
    Delivered,
    Returned
}
