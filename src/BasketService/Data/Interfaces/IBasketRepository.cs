
using Domain;

namespace Data.Interfaces
{
    public interface IBasketRepository
    {
        public Basket GetBasketByClientId(Guid userId);

        public int AddProductToBasket(Guid userId, Item item);
        public int RemoveProductFromBasket(Guid userId, Item item);
    }
}