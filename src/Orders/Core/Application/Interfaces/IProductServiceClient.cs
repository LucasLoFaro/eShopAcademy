using Domain.Orders.Entities;

namespace Core.Application.Interfaces;

public interface IProductServiceClient
{
    Task<Product> GetProductByIdAsync(Guid productId);
}