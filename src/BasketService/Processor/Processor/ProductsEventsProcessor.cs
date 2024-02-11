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
        public void UpdateProductData([RabbitMQTrigger("products", ConnectionStringSetting = "rabbitmq")] Product product)
        {
            _logger.LogInformation($"C# Queue trigger function processed: {product.ID}");
            _productRepository.AddOrUpdateProduct(product);
        }

        [Function("UpdateStock")]
        public void UpdateStock([RabbitMQTrigger("stock", ConnectionStringSetting = "rabbitmq")] Product product)
        {
            _logger.LogInformation($"C# Queue trigger function processed: {product.ID}");
            throw new NotImplementedException();
        }
    }
}
