using Core.Application.Interfaces.Services;
using Domain.Product.Entities;
using Domain.Common.Events;
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
            ProductId = product.Id,
            Name = product.Name,
            Price = product.Price,
            EventType = ProductEventType.Updated
        };

        await _publishEndpoint.Publish(command, ct);
    }

    public async Task SendProductDelete(Product product, CancellationToken ct = default)
    {
        var command = new ProductUpdatedEvent()
        {
            ProductId = product.Id,
            Name = product.Name,
            Price = product.Price,
            EventType = ProductEventType.Deleted
        };

        await _publishEndpoint.Publish(command, ct);
    }
}
