using Domain.Operations.Contracts;
using Domain.Operations.Entities;

namespace Operations.Application.Services;

public interface IPackageWorkflowService
{
    Task<IReadOnlyList<Package>> GetPendingPackagesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Package>> GetPendingPackagesBySellerAsync(Guid sellerId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Package>> GetPackagesBySellerAsync(Guid sellerId, int limit, CancellationToken cancellationToken);
    Task<Package?> StartProcessingBySellerAsync(Guid orderId, Guid sellerId, CancellationToken cancellationToken);
    Task<Package?> ReportProblemBySellerAsync(Guid orderId, Guid sellerId, ReportPackageProblemRequest request, CancellationToken cancellationToken);
    Task<Package> StartProcessingAsync(Guid orderId, StartPackageProcessingRequest request, CancellationToken cancellationToken);
    Task<Package> MarkReadyForPickupAsync(Guid orderId, MarkOrderReadyRequest request, CancellationToken cancellationToken);
    Task<Package?> MarkReadyForPickupBySellerAsync(Guid orderId, Guid sellerId, MarkOrderReadyRequest request, CancellationToken cancellationToken);
    Task<Package> ReportProblemAsync(Guid orderId, ReportPackageProblemRequest request, CancellationToken cancellationToken);
}
