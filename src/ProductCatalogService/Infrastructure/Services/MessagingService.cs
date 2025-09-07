using Core.Domain.Entities;
using Core.Domain.Events;
using Core.Domain.DTOs;
using MassTransit;


namespace Infrastructure.Services;

public class ProductMessagingClient
{
    private readonly IPublishEndpoint _publishEndpoint;

    public ProductMessagingClient(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }


    public async Task SendProductUpdate(Product product, CancellationToken ct = default)
    {
        var command = new ProductUpdatedEvent()
        {
            Product = new ProductDTO()
            {
                ID = product.ID,
                Name = product.Name,
                Price = product.Price
            },
            EventType = ProductEventType.Updated
        };

        await _publishEndpoint.Publish(command, ct);
    }

    public async Task SendProductDelete(Product product, CancellationToken ct = default)
    {
        var command = new ProductUpdatedEvent()
        {
            Product = new ProductDTO()
            {
                ID = product.ID,
                Name = product.Name,
                Price = product.Price
            },
            EventType = ProductEventType.Deleted
        };

        await _publishEndpoint.Publish(command, ct);
    }
}
