using Core.Domain.Events;
using Core.Domain.DTOs;
using Infrastructure.Services.Settings;
using Microsoft.Extensions.Options;
using MassTransit;
using Infrastructure.Services.Interfaces;

namespace Infrastructure.Services
{
    public class RabbitMQClient : IMessagingServiceClient
    {
        private readonly IBusControl _bus;
        public RabbitMQClient(IOptionsMonitor<RabbitMQSettings> settings)
        {
            _bus = Bus.Factory.CreateUsingRabbitMq(cfg =>
            {
                cfg.Host(new Uri("rabbitmq://" + settings.CurrentValue.Host), h =>
                {
                    h.Username(settings.CurrentValue.Username);
                    h.Password(settings.CurrentValue.Password);
                });
            });
            _bus.Start();
        }

        public async Task SendStockUpdate(AlterStockDTO stock)
        {
            await _bus.Publish(new StockUpdatedEvent()
            {
                Stock = new AlterStockDTO()
                {
                    ProductGuid = stock.ProductGuid,
                    Quantity = stock.Quantity,
                    Warehouse = stock.Warehouse
                }
            });
        }
    }
}
