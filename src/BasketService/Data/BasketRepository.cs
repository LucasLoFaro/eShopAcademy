using Data.Interfaces;
using Domain;
using NRedisStack;
using NRedisStack.RedisStackCommands;
using StackExchange.Redis;
using System.Text.Json;

namespace Data
{
    public class BasketRepository : IBasketRepository
    {
        private IDatabase _cache;
        private const String BASKET_PREFIX = "basket:";
        private const String PRODUCT_PREFIX = "product:";

        public BasketRepository(DatabaseClient database)
        {
            _cache = database.GetDatabase();
        }

        public async Task<Basket> GetBasketByClientId(Guid clientId)
        {
            Basket? basket;
            try
            {
                var json = await _cache.StringGetAsync(BASKET_PREFIX + clientId.ToString());
                basket = JsonSerializer.Deserialize<Basket>(json);
            }
            catch (Exception)
            {
                Console.WriteLine($"There was an issue while retrieving basket {clientId} from cache.", clientId);
                return null;
            }

            // Accumulate all product queries in a single bulk db interaction
            var batch = _cache.CreateBatch();
            var productTasks = new Dictionary<Guid, Task<HashEntry[]>>();

            foreach (var item in basket.Items)
            {
                // First initialize it with default/zero values
                item.Product = new Product {
                    ID = item.Product.ID,
                    Name = "",
                    Price = 0,
                    Stock = 0
                };
                // Then query the actual name and price values from the products collection
                productTasks[item.Product.ID] = batch.HashGetAllAsync(PRODUCT_PREFIX + item.Product.ID);
            }

            // Run all queries at once and wait for all of them to finish.
            batch.Execute();
            await Task.WhenAll(productTasks.Values);

            // Load the actual name and price values in each item
            foreach (var item in basket.Items)
            {
                var productHash = productTasks[item.Product.ID].Result;
                item.Product.Name = productHash.FirstOrDefault(h => h.Name == "Name").ToString();
                item.Product.Price = Convert.ToDouble(productHash.FirstOrDefault(h => h.Name == "Price").Value.ToString());
                item.Product.Stock = Convert.ToInt32(productHash.FirstOrDefault(h => h.Name == "Stock").Value.ToString());
            }

            return basket;
        }
        public async Task<bool> AddProductToBasket(Guid clientId, Item item)
        {
            var basket = await GetBasketByClientId(clientId);
            if (basket == null)
            {
                basket = new Basket()
                {
                    ClientID = clientId,
                    Items = new List<Item>() { item }
                };
            }
            else
            {
                var existingItem = basket.Items.FirstOrDefault(i => i.Product.ID == item.Product.ID);
                if (existingItem != null)
                    existingItem.Quantity += item.Quantity;
                else
                    basket.Items.Add(item);
            }

            try
            {
                _cache.StringSet(BASKET_PREFIX + clientId.ToString(), JsonSerializer.Serialize(basket));
            }
            catch (Exception)
            {
                Console.WriteLine("There was an issue while storing the basket");
                return false;
            }

            return true;
        }
        public async Task<bool> RemoveProductFromBasket(Guid clientId, Item item)
        {
            var basket = await GetBasketByClientId(clientId);
            if (basket == null)
                return false;

            var existingItem = basket.Items.FirstOrDefault(i => i.Product.ID == item.Product.ID);
            if (existingItem == null)
                return false;
            
            if (existingItem.Quantity - item.Quantity > 0)
                existingItem.Quantity -= item.Quantity;
            else
                basket.Items.Remove(existingItem);

            try
            {
                _cache.StringSet(BASKET_PREFIX + clientId.ToString(), JsonSerializer.Serialize(basket));
            }
            catch (Exception)
            {
                Console.WriteLine("There was an issue while caching the basket");
                return false;
            }

            return true;
        }
    }
}