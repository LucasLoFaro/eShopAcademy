using Infrastructure.Services.Interfaces;
using Infrastructure.Services.Settings;
using Microsoft.Extensions.Options;
using Core.Domain.Events;
using Core.Domain.DTOs;
using Azure.Identity;
using MassTransit;


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

    public async Task SendStockUpdate(AlterStockDTO stock)
    {
        await _bus.Publish(new StockUpdatedEvent
        {
            Stock = new AlterStockDTO
            {
                ProductGuid = stock.ProductGuid,
                Quantity = stock.Quantity,
                Warehouse = stock.Warehouse
            }
        });
    }
}
