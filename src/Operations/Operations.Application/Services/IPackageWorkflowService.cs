using Domain.Operations.Contracts;
using Domain.Operations.Entities;

namespace Operations.Application.Services;

public interface IPackageWorkflowService
{
    Task<IReadOnlyList<Package>> GetPendingPackagesAsync(CancellationToken cancellationToken);
    Task<Package> StartProcessingAsync(Guid orderId, StartPackageProcessingRequest request, CancellationToken cancellationToken);
    Task<Package> MarkReadyForPickupAsync(Guid orderId, MarkOrderReadyRequest request, CancellationToken cancellationToken);
    Task<Package> ReportProblemAsync(Guid orderId, ReportPackageProblemRequest request, CancellationToken cancellationToken);
}
