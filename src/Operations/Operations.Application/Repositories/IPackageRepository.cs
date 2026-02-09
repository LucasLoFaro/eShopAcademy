using Domain.Operations.Entities;

namespace Operations.Application.Repositories;

public interface IPackageRepository
{
    Task<IReadOnlyList<Package>> GetPendingAsync(CancellationToken cancellationToken);
    Task<Package?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);
    Task<Package> CreateOrUpdateAsync(Package package, CancellationToken cancellationToken);
}
