using Common.Domain.Events.Orders;
using Core.Application.Interfaces;
using Domain.Orders.Entities;
using MassTransit;

namespace Infrastructure.Services;

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
            CustomerEmail = order.Customer?.Mail ?? string.Empty
        }, ct);

    public Task PublishOrderCancelled(Guid orderId, string customerEmail, string reason, CancellationToken ct = default)
        => _publishEndpoint.Publish(new OrderCancelledEvent
        {
            OrderId = orderId,
            CustomerEmail = customerEmail,
            Reason = reason
        }, ct);
}