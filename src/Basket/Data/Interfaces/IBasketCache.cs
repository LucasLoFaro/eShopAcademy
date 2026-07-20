using Domain.Basket.Entities;

namespace Data.Interfaces;

public interface IBasketCache
{
    Task<BasketWithDetails> GetBasketLoadedByClientId(Guid clientId, CancellationToken cancellationToken = default);
    Task<bool> AddProductToBasket(Guid clientId, Item item, CancellationToken cancellationToken = default);
    Task<bool> RemoveProductFromBasket(Guid clientId, Item item, CancellationToken cancellationToken = default);
    Task<bool> EmptyBasket(Guid clientId, CancellationToken cancellationToken = default);
    Task<bool> ReinstateBasket(Guid clientId, IReadOnlyCollection<Item> items, CancellationToken cancellationToken = default);
}
