using Domain.Common.Events.Operations;
using Domain.Common.Events.Orders;
using Domain.Operations.Contracts;
using Domain.Operations.Entities;
using Domain.Operations.Enums;
using MassTransit;
using Microsoft.Extensions.Logging;
using Operations.Application.Repositories;

namespace Operations.Application.Services;

public class PackageWorkflowService : IPackageWorkflowService
{
    private readonly IPackageRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<PackageWorkflowService> _logger;

    public PackageWorkflowService(
        IPackageRepository repository,
        IPublishEndpoint publishEndpoint,
        ILogger<PackageWorkflowService> logger)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public Task<IReadOnlyList<Package>> GetPendingPackagesAsync(CancellationToken cancellationToken)
        => _repository.GetPendingAsync(cancellationToken);

    public Task<Package> StartProcessingAsync(Guid orderId, StartPackageProcessingRequest request, CancellationToken cancellationToken)
        => UpdatePackageAsync(orderId, request.ReservationId, request.CustomerName, request.CustomerEmail, PackageStatus.Preparing, cancellationToken, setReadyAt: false);

    public async Task<Package> MarkReadyForPickupAsync(Guid orderId, MarkOrderReadyRequest request, CancellationToken cancellationToken)
    {
        var package = await UpdatePackageAsync(orderId, null, request.CustomerName, request.CustomerEmail, PackageStatus.ReadyForPickup, cancellationToken, setReadyAt: true);

        await _publishEndpoint.Publish<OrderReadyForPickupEvent>(new
        {
            OrderId = package.OrderId,
            package.CustomerName,
            package.CustomerEmail,
            ReadyAt = package.ReadyAt ?? DateTime.UtcNow
        }, cancellationToken);

        return package;
    }

    public async Task<Package> ReportProblemAsync(Guid orderId, ReportPackageProblemRequest request, CancellationToken cancellationToken)
    {
        var package = await _repository.GetByOrderIdAsync(orderId, cancellationToken) ?? new Package
        {
            OrderId = orderId
        };

        package.Status = PackageStatus.Failed;
        package.IssueType = string.IsNullOrWhiteSpace(request.IssueType) ? "Unspecified" : request.IssueType!;
        package.IssueDetails = request.Details ?? string.Empty;
        package.ReportedBy = request.ReportedBy ?? string.Empty;
        package.IssueReportedAt = DateTime.UtcNow;
        package.UpdatedAt = DateTime.UtcNow;

        var updated = await _repository.CreateOrUpdateAsync(package, cancellationToken);

        await _publishEndpoint.Publish<PackageIssueReportedEvent>(new
        {
            OrderId = updated.OrderId,
            updated.CustomerName,
            updated.CustomerEmail,
            IssueType = updated.IssueType,
            Details = updated.IssueDetails,
            ReportedBy = updated.ReportedBy,
            ReportedAt = updated.IssueReportedAt ?? DateTime.UtcNow,
            ReservationId = updated.ReservationId
        }, cancellationToken);

        _logger.LogWarning(
            "[Operations] Problem reported for order {OrderId}: {Issue} - {Details}",
            orderId,
            updated.IssueType,
            updated.IssueDetails);

        return updated;
    }

    private async Task<Package> UpdatePackageAsync(Guid orderId, Guid? reservationId, string? customerName, string? customerEmail, PackageStatus status, CancellationToken cancellationToken, bool setReadyAt)
    {
        var package = await _repository.GetByOrderIdAsync(orderId, cancellationToken) ?? new Package
        {
            OrderId = orderId
        };

        if (reservationId.HasValue)
        {
            package.ReservationId = reservationId;
            package.PreparedAt ??= DateTime.UtcNow;
        }

        if (!string.IsNullOrWhiteSpace(customerName))
        {
            package.CustomerName = customerName;
        }

        if (!string.IsNullOrWhiteSpace(customerEmail))
        {
            package.CustomerEmail = customerEmail;
        }

        package.Status = status;

        if (setReadyAt)
        {
            package.ReadyAt = DateTime.UtcNow;
        }

        package.UpdatedAt = DateTime.UtcNow;

        return await _repository.CreateOrUpdateAsync(package, cancellationToken);
    }
}
