using Domain.Basket.Entities;

namespace Data.Interfaces;

public interface IBasketCache
{
    public Task<BasketWithDetails> GetBasketLoadedByClientId(Guid userId);
    public Task<bool> AddProductToBasket(Guid userId, Item item);
    public Task<bool> RemoveProductFromBasket(Guid userId, Item item);
}