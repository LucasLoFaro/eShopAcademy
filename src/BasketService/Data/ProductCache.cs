using Data.Interfaces;
using Core.Domain.Contracts;
using Core.Domain.Entities;
using NRedisStack;
using NRedisStack.RedisStackCommands;
using StackExchange.Redis;
using System.Text.Json;
using Domain.DTOs;

namespace Data
{
    public class ProductCache : IProductCache
    {
        private IDatabase _cache;
        private const String PRODUCT_PREFIX = "product:";

        public ProductCache(IDatabaseClient database)
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
            Console.WriteLine($"Product {product.Name} updated in cache.");
            return true;
        }
        public async Task<bool> UpdateProductStock(AlterStockDTO stock)
        {
            await _cache.HashSetAsync(PRODUCT_PREFIX + stock.ProductGuid.ToString(),new RedisValue("Stock"), stock.Quantity);
            Console.WriteLine($"Stock of {stock.ProductGuid} updated in cache.");
            return true;
        }
    }
}
