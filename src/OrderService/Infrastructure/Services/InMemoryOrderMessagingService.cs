using Core.Domain.Commands;
using Core.Domain.Contracts;
using Infrastructure.Services.Interfaces;
using MassTransit;

namespace Infrastructure.Services;

/// <summary>
/// Implementation of <see cref="IOrderMessagingService"/> that uses the
/// MassTransit publish endpoint to publish order commands onto the
/// configured message bus.  When running with the in-memory transport
/// configured by <c>DevelopmentServiceConfiguration</c>, this will
/// publish to the local in-memory bus instead of Azure Service Bus.
/// </summary>
public class InMemoryOrderMessagingService : IOrderMessagingService
{
    private readonly IPublishEndpoint _publishEndpoint;

    public InMemoryOrderMessagingService(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    /// <inheritdoc />
    public Task SubmitOrder(OrderRequest orderRequest)
    {
        var command = new SubmitOrder
        {
            OrderId = Guid.NewGuid(),
            CustomerId = orderRequest.CustomerId,
            Items = orderRequest.Items
        };

        return _publishEndpoint.Publish(command);
    }
}