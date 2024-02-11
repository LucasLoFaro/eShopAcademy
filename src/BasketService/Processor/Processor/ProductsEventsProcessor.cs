using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Domain;
using Data;
using Data.Interfaces;

namespace Processor
{
    public class ProductsEventsProcessor
    {
        private readonly ILogger _logger;
        private readonly IProductRepository _productRepository;

        public ProductsEventsProcessor(ILoggerFactory loggerFactory, IProductRepository productRepository)
        {
            _logger = loggerFactory.CreateLogger<ProductsEventsProcessor>();
            _productRepository = productRepository;
        }

        [Function("UpdateProductData")]
        public void UpdateProductData([RabbitMQTrigger(queueName:"products", HostName = "rabbitmq", UserNameSetting = "guest",PasswordSetting ="guest")] ProductDTO product)
        {
            _logger.LogInformation($"Updating product data for: {product.ID}");
            _productRepository.AddOrUpdateProduct(product);
        }

        [Function("UpdateStock")]
        public void UpdateStock([RabbitMQTrigger("stock", ConnectionStringSetting = "rabbitmq")] ProductStockDTO stock)
        {
            _logger.LogInformation($"Updating stock for: {stock.ProductID}");
            _productRepository.UpdateProductStock(stock);
        }
    }
}
