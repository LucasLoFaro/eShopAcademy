using Domain.Basket.Entities;
using System.Collections.Generic;

namespace Data.Interfaces;

public interface IBasketCache
{
    public Task<BasketWithDetails> GetBasketLoadedByClientId(Guid userId);
    public Task<bool> AddProductToBasket(Guid userId, Item item);
    public Task<bool> RemoveProductFromBasket(Guid userId, Item item);
    public Task<bool> EmptyBasket(Guid userId);
    public Task<bool> ReinstateBasket(Guid userId, IReadOnlyCollection<Item> items);
}