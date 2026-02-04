using Domain.Common.Commands.Orders;
using Domain.Common.Commands.Payments;
using Domain.Common.Commands.Stock;
using Domain.Common.Events.Orders;
using Data;
using Domain.Orders.Enums;
using MassTransit;

namespace Orders.Orchestration.Consumers;

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
        var customerEmail = !string.IsNullOrWhiteSpace(order.Customer?.Mail)
            ? order.Customer.Mail
            : command.CustomerEmail;
        var customerName = !string.IsNullOrWhiteSpace(order.Customer?.Name)
            ? order.Customer.Name
            : command.CustomerName;

        await context.Publish(new OrderCancelledEvent
        {
            OrderId = order.Id,
            CustomerName = customerName,
            CustomerEmail = customerEmail,
            Reason = reason
        }, context.CancellationToken);

        if (order.ReservationId != Guid.Empty)
        {
            await context.Publish(new ReleaseStockReservationCommand
            {
                OrderId = order.Id,
                ReservationId = order.ReservationId,
                Reason = reason
            }, context.CancellationToken);
        }

        if (order.PaymentId != Guid.Empty)
        {
            await context.Publish(new RefundPaymentCommand
            {
                OrderId = order.Id,
                PaymentId = order.PaymentId,
                Reason = reason
            }, context.CancellationToken);
        }

        _logger.LogInformation("[CancelOrder] Order {OrderId} cancelled and compensating commands dispatched.", order.Id);
    }
}
