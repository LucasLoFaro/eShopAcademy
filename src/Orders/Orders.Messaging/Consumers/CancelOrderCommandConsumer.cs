using Domain.Common.Commands.Orders;
using Domain.Common.Commands.Payments;
using Domain.Common.Commands.Stock;
using Domain.Common.Events.Orders;
using Infrastructure.Data;
using Domain.Orders.Enums;
using MassTransit;
using Application.Observability;

namespace Orders.Messaging.Consumers;

public sealed class CancelOrderCommandConsumer : IConsumer<CancelOrderCommand>
{
    private readonly IOrderRepository _orders;
    private readonly ILogger<CancelOrderCommandConsumer> _logger;

    public CancelOrderCommandConsumer(
        IOrderRepository orders,
        ILogger<CancelOrderCommandConsumer> logger)
    {
        _orders = orders;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CancelOrderCommand> context)
    {
        var command = context.Message;
        var order = await _orders.GetByIdAsync(command.OrderId, context.CancellationToken);
        if (order == null)
        {
            _logger.LogWarning("[CancelOrder] Order {OrderId} not found.", command.OrderId);
            return;
        }

        if (order.Status is OrderStatus.Cancelled or OrderStatus.Delivered)
        {
            _logger.LogInformation("[CancelOrder] Order {OrderId} already in state {Status}.", order.Id, order.Status);
            return;
        }

        order.Status = OrderStatus.Cancelled;
        await _orders.UpdateAsync(order, context.CancellationToken);

        var reason = string.IsNullOrWhiteSpace(command.Reason) ? "Payment failure" : command.Reason;
        var customerEmail = !string.IsNullOrWhiteSpace(order.Customer?.Email)
            ? order.Customer.Email
            : command.CustomerEmail;
        var customerName = !string.IsNullOrWhiteSpace(order.Customer?.Name)
            ? order.Customer.Name
            : command.CustomerName;

        var cancelledEventId = OrderMessageIdentity.Create(order.Id, command.EventId, "order-cancelled");
        await context.Publish(new OrderCancelledEvent
        {
            EventId = cancelledEventId,
            CorrelationId = order.Id,
            OrderId = order.Id,
            CustomerName = customerName,
            CustomerEmail = customerEmail,
            Reason = reason
        }, context.CancellationToken);

        var statusEventId = OrderMessageIdentity.Create(order.Id, command.EventId, "status-cancelled");
        await context.Publish(new OrderStatusUpdatedEvent
        {
            EventId = statusEventId,
            CorrelationId = order.Id,
            OrderId = order.Id,
            CustomerName = customerName,
            CustomerEmail = customerEmail,
            Status = "Cancelled",
            Reason = reason
        }, context.CancellationToken);

        if (order.Stock?.ReservationId is { } reservationId && reservationId != Guid.Empty)
        {
            var releaseStock = await context.GetSendEndpoint(new Uri("queue:release-stock-reservation"));
            await releaseStock.Send(new ReleaseStockReservationCommand
            {
                EventId = OrderMessageIdentity.Create(order.Id, command.EventId, "release-stock"),
                CorrelationId = order.Id,
                OrderId = order.Id,
                ReservationId = reservationId,
                Reason = reason
            }, context.CancellationToken);
        }

        if (order.Payment?.Id is { } paymentId && paymentId != Guid.Empty)
        {
            var refundPayment = await context.GetSendEndpoint(new Uri("queue:refund-payment"));
            await refundPayment.Send(new RefundPaymentCommand
            {
                EventId = OrderMessageIdentity.Create(order.Id, command.EventId, "refund-payment"),
                CorrelationId = order.Id,
                OrderId = order.Id,
                PaymentId = paymentId,
                Reason = reason
            }, context.CancellationToken);
        }

        _logger.LogInformation("[CancelOrder] Order {OrderId} cancelled and compensating commands dispatched.", order.Id);
        OrdersTelemetry.RecordResult("cancel-order", "cancelled");
    }
}
