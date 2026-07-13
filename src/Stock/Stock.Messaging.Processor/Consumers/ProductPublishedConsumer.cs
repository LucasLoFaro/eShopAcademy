using Domain.Common.Events.Products;
using Infrastructure.Data;
using Infrastructure.Services;
using Domain.Stock.Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Stock.Messaging.Processor.Consumers;

public sealed class ProductPublishedConsumer : IConsumer<ProductPublishedEvent>
{
    private readonly IStockRepository _stockRepository;
    private readonly StockMessagingClient _messagingClient;
    private readonly ILogger<ProductPublishedConsumer> _logger;

    public ProductPublishedConsumer(
        IStockRepository stockRepository,
        StockMessagingClient messagingClient,
        ILogger<ProductPublishedConsumer> logger)
    {
        _stockRepository = stockRepository;
        _messagingClient = messagingClient;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProductPublishedEvent> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "[Stock] Product published event received for product {ProductId}. Loading into local cache.",
            message.ProductId);

        var existing = await _stockRepository.GetByProductIdAsync(message.ProductId, context.CancellationToken);

        if (existing is not null)
        {
            _logger.LogInformation(
                "[Stock] Stock entry already exists for product {ProductId} with quantity {Quantity}.",
                message.ProductId, existing.Quantity);
            return;
        }

        var stock = new Domain.Stock.Entities.Stock
        {
            ProductID = message.ProductId,
            Quantity = 0,
            Warehouse = "WH-01"
        };

        await _stockRepository.AddOrUpdateAsync(stock, context.CancellationToken);

        await _messagingClient.SendStockUpdate(new AlterStockRequest(stock), context.CancellationToken);

        _logger.LogInformation(
            "[Stock] Product {ProductId} loaded into stock cache with 0 units.",
            message.ProductId);
    }
}
