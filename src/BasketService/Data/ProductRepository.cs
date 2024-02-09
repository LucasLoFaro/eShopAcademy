using Data.Interfaces;
using Domain;
using NRedisStack;
using NRedisStack.RedisStackCommands;
using StackExchange.Redis;
using System.Text.Json;

namespace Data
{
    public class ProductRepository : IProductRepository
    {
        private IDatabase _cache;
        private const String PRODUCT_PREFIX = "product:";

        public ProductRepository(DatabaseClient database)
        {
            _cache = database.GetDatabase();
        }

        public async Task<bool> AddOrUpdateProduct(Product product)
        {
            HashEntry[] productHash = {
                new HashEntry("ID", product.ID.ToString()),
                new HashEntry("Name", product.Name),
                new HashEntry("Price", product.Price)
            };

            await _cache.HashSetAsync(PRODUCT_PREFIX + product.ID.ToString(), productHash);
            return true;
        }
        public async Task<bool> UpdateProductStock(ProductStockDTO stock)
        {
            HashEntry[] productStockHash = {
                new HashEntry("ProductID", stock.ProductID.ToString()),
                new HashEntry("Stock", stock.Stock)
            };

            await _cache.HashSetAsync(PRODUCT_PREFIX + stock.ProductID.ToString(), productStockHash);
            return true;
        }
    }
}
