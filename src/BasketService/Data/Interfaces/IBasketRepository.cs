
using Domain;

namespace Data.Interfaces
{
    public interface IBasketRepository
    {
        public Task<Basket> GetBasketByClientId(Guid userId);
        public Task<bool> AddProductToBasket(Guid userId, Item item);
        public Task<bool> RemoveProductFromBasket(Guid userId, Item item);
    }
}