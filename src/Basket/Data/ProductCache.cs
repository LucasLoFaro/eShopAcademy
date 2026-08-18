using Data.Interfaces;
using Domain.Basket.Contracts;
using StackExchange.Redis;
using Microsoft.Extensions.Logging;


namespace Data;

public class ProductCache : IProductCache
{
    private readonly IDatabase _cache;
    private readonly ILogger<ProductCache> _logger;
    private const String PRODUCT_PREFIX = "product:";

    public ProductCache(IDatabaseClient database, ILogger<ProductCache> logger)
    {
        _cache = database.GetDatabase();
        _logger = logger;
    }

    public async Task<bool> AddOrUpdateProduct(ProductDTO product, CancellationToken cancellationToken = default)
    {
        HashEntry[] productHash = {
            new HashEntry("ID", product.ID.ToString()),
            new HashEntry("Name", product.Name),
            new HashEntry("Price", product.Price)
        };

        await _cache.HashSetAsync(PRODUCT_PREFIX + product.ID.ToString(), productHash).WaitAsync(cancellationToken);
        _logger.LogInformation("Updated product {ProductId} in basket cache", product.ID);
        return true;
    }
    public async Task<bool> UpdateProductStock(AlterStockDTO stock, CancellationToken cancellationToken = default)
    {
        await _cache.HashSetAsync(PRODUCT_PREFIX + stock.ProductGuid.ToString(),new RedisValue("Stock"), stock.Quantity).WaitAsync(cancellationToken);
        _logger.LogInformation("Updated stock for product {ProductId} in basket cache", stock.ProductGuid);
        return true;
    }
}
