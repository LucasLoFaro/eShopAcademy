using Core.Application.Interfaces;
using Domain.Common.Events.Orders;
using Domain.Orders.Entities;
using MassTransit;

namespace Infrastructure.Clients;

public sealed class OrderMessagingClient : IOrderMessagingClient
{
    private readonly IPublishEndpoint _publishEndpoint;

    public OrderMessagingClient(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public Task PublishOrderSubmitted(Order order, CancellationToken ct = default)
        => _publishEndpoint.Publish(new OrderSubmittedEvent
        {
            OrderId = order.Id,
            CustomerName = order.Customer?.Name ?? string.Empty,
            CustomerEmail = order.Customer?.Mail ?? string.Empty,
            CustomerId = order.CustomerId,
            TotalAmount = Convert.ToDecimal(order.TotalPrice),
            PaymentId = order.PaymentId,
            ReservationId = order.ReservationId
        }, ct);

    public Task PublishOrderCancelled(Guid orderId, string customerEmail, string reason, CancellationToken ct = default)
        => _publishEndpoint.Publish(new OrderCancelledEvent
        {
            OrderId = orderId,
            CustomerEmail = customerEmail,
            Reason = reason
        }, ct);
}