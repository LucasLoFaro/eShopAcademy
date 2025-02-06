//using Core.Domain.Events;
//using Core.Domain.Entities;
//using Core.Application.Interfaces.Services;
//using Core.Domain.DTOs;
//using Services.Settings;
//using Microsoft.Extensions.Options;
//using MassTransit;

//namespace Services
//{
//    public class RabbitMQClient : IMessagingServiceClient
//    {
//        private readonly IBusControl _bus;
//        public RabbitMQClient(IOptionsMonitor<RabbitMQSettings> settings)
//        {
//            _bus = Bus.Factory.CreateUsingRabbitMq(cfg =>
//            {
//                cfg.Host(new Uri("rabbitmq://" + settings.CurrentValue.Host), h =>
//                {
//                    h.Username(settings.CurrentValue.Username);
//                    h.Password(settings.CurrentValue.Password);
//                });
//            });
//            _bus.Start();
//        }
        
//        public async Task SendProductUpdate(Product product)
//        {
//            await _bus.Publish(new ProductUpdatedEvent()
//            {
//                Product = new ProductDTO()
//                {
//                    ID = product.ID,
//                    Name = product.Name,
//                    Price = product.Price
//                },
//                EventType = ProductEventType.Updated
//            });
//        }

//        public async Task SendProductDelete(Product product)
//        {
//            await _bus.Publish(new ProductUpdatedEvent()
//            {
//                Product = new ProductDTO()
//                {
//                    ID = product.ID,
//                    Name = product.Name,
//                    Price = product.Price
//                },
//                EventType = ProductEventType.Deleted
//            });
//        }
//    }
//}
