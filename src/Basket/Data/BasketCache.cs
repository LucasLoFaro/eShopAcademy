using Data.Interfaces;
using Domain.Basket.Entities;
using DomainEntities = Domain.Basket.Entities;
using StackExchange.Redis;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Data;

public class BasketCache : IBasketCache
{
    private readonly IDatabase _cache;
    private readonly ILogger<BasketCache> _logger;
    private const String BASKET_PREFIX = "basket:";
    private const String PRODUCT_PREFIX = "product:";

    public BasketCache(IDatabaseClient database, ILogger<BasketCache> logger)
    {
        _cache = database.GetDatabase();
        _logger = logger;
    }

    public async Task<BasketWithDetails> GetBasketLoadedByClientId(Guid clientId, CancellationToken cancellationToken = default)
    {
        DomainEntities.Basket? basket;
        try
        {
            var json = await _cache.StringGetAsync(BASKET_PREFIX + clientId.ToString()).WaitAsync(cancellationToken);
            basket = JsonSerializer.Deserialize<DomainEntities.Basket>(json.ToString())!;
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Unable to retrieve a basket from Redis");
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
        await Task.WhenAll(productTasks.Values).WaitAsync(cancellationToken);

        // Load the actual name and price values in each item
        foreach (var item in basketWithDetails.Items)
        {
            var productHash = productTasks[item.Product.ID].Result;
            if(productHash != null && productHash.Length > 0)
            {
                // TODO: Add validation and encapsulate in automapper
                item.Product.Name = productHash.FirstOrDefault(h => h.Name == "Name").Value.ToString();
                var priceStr = productHash.FirstOrDefault(h => h.Name == "Price").Value.ToString();
                item.Product.Price = double.TryParse(priceStr, out var price) ? price : 0;
                var stockStr = productHash.FirstOrDefault(h => h.Name == "Stock").Value.ToString();
                item.Product.Stock = int.TryParse(stockStr, out var stock) ? stock : 0;
            }                
        }

        return basketWithDetails;
    }

    private async Task<DomainEntities.Basket> GetBasketByClientId(Guid clientId, CancellationToken cancellationToken)
    {
        try
        {
            var json = await _cache.StringGetAsync(BASKET_PREFIX + clientId.ToString()).WaitAsync(cancellationToken);
            return JsonSerializer.Deserialize<DomainEntities.Basket>(json.ToString())!;
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Unable to retrieve a basket from Redis");
            return null!;
        }
    }

    //TODO: Check stock before adding to basket
    public async Task<bool> AddProductToBasket(Guid clientId, Item item, CancellationToken cancellationToken = default)
    {
        var basket = await GetBasketByClientId(clientId, cancellationToken);
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
            await _cache.StringSetAsync(BASKET_PREFIX + clientId.ToString(), JsonSerializer.Serialize(basket)).WaitAsync(cancellationToken);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Unable to store a basket in Redis");
            return false;
        }

        return true;
    }
    public async Task<bool> RemoveProductFromBasket(Guid clientId, Item item, CancellationToken cancellationToken = default)
    {
        var basket = await GetBasketByClientId(clientId, cancellationToken);
        if (basket == null)
            return false;

        var existingItem = basket.Items.FirstOrDefault(i => i.ProductID == item.ProductID);
        if (existingItem == null)
            return false;
        
        if (existingItem.Quantity - item.Quantity > 0)
            existingItem.Quantity -= item.Quantity;
        else
            basket.Items.Remove(existingItem);

        try
        {
            await _cache.StringSetAsync(BASKET_PREFIX + clientId.ToString(), JsonSerializer.Serialize(basket)).WaitAsync(cancellationToken);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Unable to update a basket in Redis");
            return false;
        }

        return true;
    }

    public async Task<bool> ReinstateBasket(Guid clientId, IReadOnlyCollection<Item> items, CancellationToken cancellationToken = default)
    {
        var basket = new DomainEntities.Basket
        {
            ClientID = clientId,
            Items = new List<Item>(items)
        };

        try
        {
            await _cache.StringSetAsync(BASKET_PREFIX + clientId.ToString(), JsonSerializer.Serialize(basket)).WaitAsync(cancellationToken);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Unable to reinstate a basket in Redis");
            throw;
        }

        return true;
    }

    public async Task<bool> EmptyBasket(Guid clientId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _cache.KeyDeleteAsync(BASKET_PREFIX + clientId.ToString()).WaitAsync(cancellationToken);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Unable to remove a basket from Redis");
            throw;
        }
    }
}
