using Data.Interfaces;
using Domain.DTOs;
using Domain.Entities;
using NRedisStack;
using NRedisStack.RedisStackCommands;
using StackExchange.Redis;
using System.Text.Json;

namespace Data
{
    public class ProductCache : IProductCache
    {
        private IDatabase _cache;
        private const String PRODUCT_PREFIX = "product:";

        public ProductCache(DatabaseClient database)
        {
            _cache = database.GetDatabase();
        }

        public async Task<bool> AddOrUpdateProduct(ProductDTO product)
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
                new HashEntry("ID", stock.ProductID.ToString()),
                new HashEntry("Stock", stock.Stock)
            };

            await _cache.HashSetAsync(PRODUCT_PREFIX + stock.ProductID.ToString(), productStockHash);
            return true;
        }
    }
}
