using Domain.Operations.Entities;

namespace Operations.Application.Repositories;

public interface IPackageRepository
{
    Task PingAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Package>> GetPendingAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Package>> GetPendingBySellerAsync(Guid sellerId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Package>> GetBySellerAsync(Guid sellerId, int limit, CancellationToken cancellationToken);
    Task<Package?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);
    Task<Package?> GetByOrderIdAndSellerAsync(Guid orderId, Guid sellerId, CancellationToken cancellationToken);
    Task<Package> CreateOrUpdateAsync(Package package, CancellationToken cancellationToken);
}
