using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Domain;
using Data;
using Data.Interfaces;
using Domain.Events;

namespace Processor
{
    public class ProductsEventsProcessor
    {
        private readonly ILogger _logger;
        private readonly IProductCache _productCache;

        public ProductsEventsProcessor(ILoggerFactory loggerFactory, IProductCache productRepository)
        {
            _logger = loggerFactory.CreateLogger<ProductsEventsProcessor>();
            _productCache = productRepository;
        }

        [Function("UpdateProductData")]
        public void UpdateProductData([RabbitMQTrigger("product-events", HostName = "rabbitmq", UserNameSetting = "guest",PasswordSetting ="guest")] ProductUpdatedEvent productUpdatedEvent)
        {
            _logger.LogInformation($"Updating product data for: {productUpdatedEvent.Product.ID}");
            _productCache.AddOrUpdateProduct(productUpdatedEvent.Product);
        }

        //[Function("UpdateStock")]
        //public void UpdateStock([RabbitMQTrigger("stock", ConnectionStringSetting = "rabbitmq")] ProductStockDTO stock)
        //{
        //    _logger.LogInformation($"Updating stock for: {stock.ProductID}");
        //    _productRepository.UpdateProductStock(stock);
        //}
    }
}
