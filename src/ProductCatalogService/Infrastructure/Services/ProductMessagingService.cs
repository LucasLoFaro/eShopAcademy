using Core.Application.Interfaces.Services;
using Core.Domain.DTOs;
using Core.Domain.Entities;
using Core.Domain.Events;
using MassTransit;


namespace Infrastructure.Services;

public class ProductMessagingService : IProductMessagingService
{
    private readonly IPublishEndpoint _publishEndpoint;

    public ProductMessagingService(IPublishEndpoint publishEndpoint)
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
