using Core.Application.Interfaces.Services;
using Infrastructure.Services.Settings;
using Microsoft.Extensions.Options;
using Azure.Identity;
using MassTransit;
using Core.Domain.Events;
using Core.Domain.Entities;
using Core.Domain.DTOs;


namespace Infrastructure.Services;

public class ServiceBusClient : IMessagingServiceClient
{
    private readonly IBusControl _bus;

    public ServiceBusClient(IOptionsMonitor<ServiceBusSettings> settings)
    {
        var credential = new DefaultAzureCredential();

        _bus = Bus.Factory.CreateUsingAzureServiceBus(cfg =>
        {
            cfg.Host($"sb://{settings.CurrentValue.Host}/", h =>
            {
                h.TokenCredential = credential;
            });
        });

        _bus.Start();
    }

    public async Task SendProductUpdate(Product product)
    {
        await _bus.Publish(new ProductUpdatedEvent()
        {
            Product = new ProductDTO()
            {
                ID = product.ID,
                Name = product.Name,
                Price = product.Price
            },
            EventType = ProductEventType.Updated
        });
    }

    public async Task SendProductDelete(Product product)
    {
        await _bus.Publish(new ProductUpdatedEvent()
        {
            Product = new ProductDTO()
            {
                ID = product.ID,
                Name = product.Name,
                Price = product.Price
            },
            EventType = ProductEventType.Deleted
        });
    }
}
