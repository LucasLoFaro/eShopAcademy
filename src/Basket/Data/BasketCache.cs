using Data.Interfaces;
using Domain.Basket.Entities;
using DomainEntities = Domain.Basket.Entities;
using StackExchange.Redis;
using System.Text.Json;

namespace Data;

public class BasketCache : IBasketCache
{
    private IDatabase _cache;
    private const String BASKET_PREFIX = "basket:";
    private const String PRODUCT_PREFIX = "product:";

    public BasketCache(IDatabaseClient database)
    {
        _cache = database.GetDatabase();
    }

    public async Task<BasketWithDetails> GetBasketLoadedByClientId(Guid clientId)
    {
        DomainEntities.Basket? basket;
        try
        {
            var json = await _cache.StringGetAsync(BASKET_PREFIX + clientId.ToString());
            basket = JsonSerializer.Deserialize<DomainEntities.Basket>(json.ToString())!;
        }
        catch (Exception)
        {
            Console.WriteLine($"There was an issue while retrieving basket {clientId} from cache.", clientId);
            return null!;
        }


        // Accumulate all product queries in a single bulk db interaction
        var batch = _cache.CreateBatch();
        var productTasks = new Dictionary<Guid, Task<HashEntry[]>>();

        BasketWithDetails basketWithDetails = new() { ClientID = basket.ClientID };
        foreach (var item in basket.Items)
        {
            // First initialize products list it with default values
            basketWithDetails.Items.Add(new() { 
                Product = new()
                {
                    ID = item.ProductID,
                    Name = "",
                    Price = 0,
                    Stock = 0
                }, 
                Quantity = item.Quantity
            });

            // Then query the actual name and price values from the products collection all at once
            productTasks[item.ProductID] = batch.HashGetAllAsync(PRODUCT_PREFIX + item.ProductID);
        }

        // Run all queries at once and wait for all of them to finish.
        batch.Execute();
        await Task.WhenAll(productTasks.Values);

        // Load the actual name and price values in each item
        foreach (var item in basketWithDetails.Items)
        {
            var productHash = productTasks[item.Product.ID].Result;
            if(productHash != null && productHash.Length > 0)
            {
                // TODO: Add validation and encapsulate in automapper
                item.Product.Name = productHash.FirstOrDefault(h => h.Name == "Name").ToString() ?? "";
                item.Product.Price = Convert.ToDouble(productHash.FirstOrDefault(h => h.Name == "Price").Value.ToString() ?? "0");
                item.Product.Stock = Convert.ToInt32(productHash.FirstOrDefault(h => h.Name == "Stock").Value.ToString() ?? "0");
            }                
        }

        return basketWithDetails;
    }

    public async Task<DomainEntities.Basket> GetBasketByClientId(Guid clientId)
    {
        try
        {
            var json = await _cache.StringGetAsync(BASKET_PREFIX + clientId.ToString());
            return JsonSerializer.Deserialize<DomainEntities.Basket>(json.ToString())!;
        }
        catch (Exception)
        {
            Console.WriteLine($"There was an issue while retrieving basket {clientId} from cache.", clientId);
            return null!;
        }
    }

    //TODO: Check stock before adding to basket
    public async Task<bool> AddProductToBasket(Guid clientId, Item item)
    {
        var basket = await GetBasketByClientId(clientId);
        if (basket == null)
        {
            basket = new DomainEntities.Basket()
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
        var basket = await GetBasketLoadedByClientId(clientId);
        if (basket == null)
            return false;

        var existingItem = basket.Items.FirstOrDefault(i => i.Product.ID == item.ProductID);
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

    public async Task<bool> ReinstateBasket(Guid clientId, IReadOnlyCollection<Item> items)
    {
        var basket = new DomainEntities.Basket
        {
            ClientID = clientId,
            Items = new List<Item>(items)
        };

        try
        {
            await _cache.StringSetAsync(BASKET_PREFIX + clientId.ToString(), JsonSerializer.Serialize(basket));
        }
        catch (Exception)
        {
            Console.WriteLine("There was an issue while storing the basket");
            return false;
        }

        return true;
    }

    public async Task<bool> EmptyBasket(Guid clientId)
    {
        try
        {
            return await _cache.KeyDeleteAsync(BASKET_PREFIX + clientId);
        }
        catch (Exception)
        {
            Console.WriteLine("There was an issue while removing the basket");
            return false;
        }
    }
}