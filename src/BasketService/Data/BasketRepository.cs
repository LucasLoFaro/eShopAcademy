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

        public BasketRepository(DatabaseClient database)
        {
            _cache = database.GetDatabase();
        }

        public Basket GetBasketByClientId(Guid clientId)
        {
            Basket basket = null;
            try
            {
                String cachedKey = _cache.StringGet(clientId.ToString());
                if (cachedKey != null)
                    basket = JsonSerializer.Deserialize<Basket>(cachedKey);
            }
            catch (Exception)
            {
                Console.WriteLine("There was an issue while retrieving the basket from the cache");
            }
            return basket;
        }
        public int AddProductToBasket(Guid clientId, Item item)
        {
            var basket = GetBasketByClientId(clientId);
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
                var existingItem = basket.Items.FirstOrDefault(i => i.ProductID == item.ProductID);
                if (existingItem != null)
                    existingItem.Quantity += item.Quantity;
                else
                    basket.Items.Add(item);
            }

            try
            {
                _cache.StringSet(clientId.ToString(), JsonSerializer.Serialize(basket));
            }
            catch (Exception)
            {
                Console.WriteLine("There was an issue while caching the basket");
                return -1;
            }

            return 0;
        }
        public int RemoveProductFromBasket(Guid clientId, Item item)
        {
            var basket = GetBasketByClientId(clientId);
            if (basket == null)
                return -1;

            var existingItem = basket.Items.FirstOrDefault(i => i.ProductID == item.ProductID);
            if (existingItem != null && existingItem.Quantity > 0) 
                existingItem.Quantity -= item.Quantity;
            else
                basket.Items.Remove(item);

            try
            {
                _cache.StringSet(clientId.ToString(), JsonSerializer.Serialize(basket));
            }
            catch (Exception)
            {
                Console.WriteLine("There was an issue while caching the basket");
                return -1;
            }

            return 0;
        }
    }
}
