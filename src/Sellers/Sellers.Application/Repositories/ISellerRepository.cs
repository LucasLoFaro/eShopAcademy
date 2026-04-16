using Domain.Sellers.Entities;

namespace Sellers.Application.Repositories;

public interface ISellerRepository
{
    Task<Seller> CreateAsync(Seller seller, CancellationToken cancellationToken);
    Task<Seller?> GetByIdAsync(Guid sellerId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Seller>> GetAllAsync(CancellationToken cancellationToken);
    Task<Seller> UpdateAsync(Seller seller, CancellationToken cancellationToken);
}
