using Core.Domain.Entities;

namespace Data.Interfaces
{
    public interface IBasketCache
    {
        public Task<Basket> GetBasketByClientId(Guid userId);
        public Task<bool> AddProductToBasket(Guid userId, Item item);
        public Task<bool> RemoveProductFromBasket(Guid userId, Item item);
    }
}