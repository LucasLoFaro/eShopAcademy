using Core.Domain.Contracts;
using Core.Domain.Commands;
using MassTransit;


namespace Infrastructure.Services;

public class OrderMessagingClient
{
    private readonly IPublishEndpoint _publishEndpoint;

    public OrderMessagingClient(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }


    public async Task SubmitOrder(OrderRequest orderRequest, CancellationToken ct = default)
    {
        // Generate a new correlation identifier for the order.  This
        // identifier will be used by the saga state machine to track
        // progress across multiple services.
        var command = new SubmitOrder
        {
            OrderId = Guid.NewGuid(),
            CustomerId = orderRequest.CustomerId,
            Items = orderRequest.Items
        };

        await _publishEndpoint.Publish(orderRequest, ct);
    }
}