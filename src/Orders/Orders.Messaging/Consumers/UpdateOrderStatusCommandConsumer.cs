using Domain.Common.Commands.Orders;
using Domain.Common.Events.Orders;
using Domain.Orders.Entities;
using Domain.Orders.Enums;
using Infrastructure.Data;
using MassTransit;

namespace Orders.Messaging.Consumers;

public sealed class UpdateOrderStatusCommandConsumer : IConsumer<UpdateOrderStatusCommand>
{
    private readonly IOrderRepository _orders;
    private readonly ILogger<UpdateOrderStatusCommandConsumer> _logger;
    private readonly decimal _sellerCommissionRate;

    public UpdateOrderStatusCommandConsumer(
        IOrderRepository orders,
        IConfiguration configuration,
        ILogger<UpdateOrderStatusCommandConsumer> logger)
    {
        _orders = orders;
        _logger = logger;
        _sellerCommissionRate = configuration.GetValue<decimal?>("Sellers:CommissionRate") ?? 0.10m;
    }

    public async Task Consume(ConsumeContext<UpdateOrderStatusCommand> context)
    {
        var command = context.Message;

        if (!Enum.TryParse<OrderStatus>(command.Status, ignoreCase: true, out var newStatus))
        {
            _logger.LogWarning("[UpdateOrderStatus] Unknown status '{Status}' for order {OrderId}.", command.Status, command.OrderId);
            return;
        }

        var order = await _orders.GetByIdAsync(command.OrderId, context.CancellationToken);
        if (order is null)
        {
            _logger.LogWarning("[UpdateOrderStatus] Order {OrderId} not found.", command.OrderId);
            return;
        }

        if (order.Status is OrderStatus.Cancelled or OrderStatus.Delivered)
        {
            _logger.LogInformation("[UpdateOrderStatus] Order {OrderId} already in terminal state {Status}.", order.Id, order.Status);
            return;
        }

        order.Status = newStatus;

        // Payment
        order.Payment ??= new OrderPaymentInfo();

        if (command.PaymentId.HasValue)
            order.Payment.Id = command.PaymentId.Value;

        if (Enum.TryParse<PaymentStatus>(command.PaymentStatus, ignoreCase: true, out var payStatus))
            order.Payment.Status = payStatus;

        if (!string.IsNullOrEmpty(command.ProviderTransactionId))
            order.Payment.ProviderTransactionId = command.ProviderTransactionId;

        if (command.Amount.HasValue)
            order.Payment.Amount = command.Amount.Value;

        if (command.PaidAt.HasValue)
            order.Payment.PaidAt = command.PaidAt;

        // Shipping
        order.Shipping ??= new OrderShippingInfo();

        if (Enum.TryParse<ShippingStatus>(command.ShippingStatus, ignoreCase: true, out var shipStatus))
            order.Shipping.Status = shipStatus;

        if (!string.IsNullOrEmpty(command.DestinationAddress))
            order.Shipping.DestinationAddress = command.DestinationAddress;

        if (!string.IsNullOrEmpty(command.TrackingNumber))
            order.Shipping.TrackingNumber = command.TrackingNumber;

        if (!string.IsNullOrEmpty(command.Carrier))
            order.Shipping.Carrier = command.Carrier;

        if (command.ReadyForPickupAt.HasValue)
            order.Shipping.ReadyForPickupAt = command.ReadyForPickupAt;

        if (command.ShippedAt.HasValue)
            order.Shipping.ShippedAt = command.ShippedAt;

        if (command.DeliveredAt.HasValue)
            order.Shipping.DeliveredAt = command.DeliveredAt;

        // Operations
        if (!string.IsNullOrEmpty(command.OperatorName) || command.PackedAt.HasValue)
        {
            order.Operations ??= new OrderOperationsInfo();

            if (!string.IsNullOrEmpty(command.OperatorName))
                order.Operations.OperatorName = command.OperatorName;

            if (command.PackedAt.HasValue)
                order.Operations.PackedAt = command.PackedAt;
        }

        // Stock
        if (command.ReservationId.HasValue || command.StockCommittedAt.HasValue)
        {
            order.Stock ??= new OrderStockReservationInfo();

            if (command.ReservationId.HasValue)
                order.Stock.ReservationId = command.ReservationId.Value;

            if (command.StockCommittedAt.HasValue)
                order.Stock.CommittedAt = command.StockCommittedAt;
        }

        await _orders.UpdateAsync(order, context.CancellationToken);

        await context.Publish(new OrderStatusUpdatedEvent
        {
            OrderId = order.Id,
            CustomerName = command.CustomerName,
            CustomerEmail = command.CustomerEmail,
            Status = command.Status,
            Amount = command.Amount,
            TrackingNumber = command.TrackingNumber,
            Carrier = command.Carrier
        }, context.CancellationToken);

        if (newStatus == OrderStatus.Paid && order.SellerSalesRegisteredAt is null)
        {
            foreach (var item in order.Items)
            {
                if (item.Product is null || item.Product.SellerId == Guid.Empty)
                {
                    _logger.LogWarning(
                        "[UpdateOrderStatus] Product {ProductId} from order {OrderId} has no SellerId. Seller sale registration skipped.",
                        item.ProductID,
                        order.Id);
                    continue;
                }

                var grossAmount = Convert.ToDecimal(item.Price);
                var commissionAmount = decimal.Round(grossAmount * _sellerCommissionRate, 2, MidpointRounding.AwayFromZero);

                await context.Publish(new OrderSellerSaleRegistrationRequestedEvent
                {
                    OrderId = order.Id,
                    SellerId = item.Product.SellerId,
                    OrderItemId = item.Id,
                    GrossAmount = grossAmount,
                    CommissionAmount = commissionAmount,
                    Notes = $"Auto-generated from payment capture. CommissionRate={_sellerCommissionRate:P2}"
                }, context.CancellationToken);
            }

            order.SellerSalesRegisteredAt = DateTime.UtcNow;
            await _orders.UpdateAsync(order, context.CancellationToken);
        }

        _logger.LogInformation("[UpdateOrderStatus] Order {OrderId} updated to {Status}.", order.Id, newStatus);
    }
}
