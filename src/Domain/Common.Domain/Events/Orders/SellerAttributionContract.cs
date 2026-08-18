using System.Security.Cryptography;
using System.Text;

namespace Domain.Common.Events.Orders;

public static class SellerAttributionContract
{
    public static Guid CreateOperationId(Guid orderId, Guid orderItemId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(orderId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(orderItemId, Guid.Empty);

        var hash = SHA256.HashData(
            Encoding.UTF8.GetBytes($"{orderId:N}:{orderItemId:N}:seller-sale"));
        return new Guid(hash.AsSpan(0, 16));
    }

    public static void Validate(OrderSubmittedEvent message)
    {
        ArgumentNullException.ThrowIfNull(message);
        ValidateOrderId(message.OrderId);

        if (message.SellerAttributions is null)
            throw new ArgumentException("Seller attribution collection is required.", nameof(message));

        var orderItems = new HashSet<Guid>();
        var operations = new HashSet<Guid>();
        foreach (var attribution in message.SellerAttributions)
        {
            ValidateAttribution(message.OrderId, attribution);

            if (!orderItems.Add(attribution.OrderItemId))
                throw new ArgumentException("Seller attribution contains a duplicate order item.", nameof(message));

            if (!operations.Add(attribution.SellerSaleOperationId))
                throw new ArgumentException("Seller attribution contains a duplicate operation identifier.", nameof(message));
        }
    }

    public static void Validate(OrderSellerSaleRegistrationRequestedEvent message)
    {
        ArgumentNullException.ThrowIfNull(message);
        ValidateOrderId(message.OrderId);
        ValidateAttribution(
            message.OrderId,
            new OrderItemSellerAttribution
            {
                OrderItemId = message.OrderItemId,
                ProductId = message.ProductId,
                SellerId = message.SellerId,
                SellerSaleOperationId = message.SellerSaleOperationId
            });
    }

    private static void ValidateAttribution(Guid orderId, OrderItemSellerAttribution attribution)
    {
        ArgumentNullException.ThrowIfNull(attribution);

        if (attribution.OrderItemId == Guid.Empty ||
            attribution.ProductId == Guid.Empty ||
            attribution.SellerId == Guid.Empty)
        {
            throw new ArgumentException(
                "Order item, product, and seller identifiers are required for seller attribution.",
                nameof(attribution));
        }

        var expectedOperationId = CreateOperationId(orderId, attribution.OrderItemId);
        if (attribution.SellerSaleOperationId != expectedOperationId)
        {
            throw new ArgumentException(
                "Seller sale operation identifier does not match the order and order item.",
                nameof(attribution));
        }
    }

    private static void ValidateOrderId(Guid orderId)
    {
        if (orderId == Guid.Empty)
            throw new ArgumentException("Order identifier is required.", nameof(orderId));
    }
}
