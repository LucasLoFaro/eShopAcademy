using AutoFixture.Xunit2;
using Domain.Common.Events.Operations;
using Domain.Common.Events.Orders;
using Domain.Operations.Contracts;
using Domain.Operations.Entities;
using Domain.Operations.Enums;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Operations.Application.Repositories;
using Operations.Application.Services;
using Xunit;

namespace Operations.Tests.Services;

public class PackageWorkflowServiceTests
{
    private readonly Mock<IPackageRepository> _repository = new();
    private readonly Mock<IPublishEndpoint> _publishEndpoint = new();

    private PackageWorkflowService CreateSut() =>
        new(_repository.Object, _publishEndpoint.Object, NullLogger<PackageWorkflowService>.Instance);

    [Theory]
    [AutoData]
    public async Task GetPendingPackagesAsync_DelegatesToRepository(List<Package> packages)
    {
        // Arrange
        _repository.Setup(r => r.GetPendingAsync(It.IsAny<CancellationToken>()))
                   .ReturnsAsync(packages);

        // Act
        var result = await CreateSut().GetPendingPackagesAsync(CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(packages);
    }

    [Theory]
    [AutoData]
    public async Task MarkReadyForPickupAsync_UpdatesStatusAndPublishesOrderReadyForPickupEvent(Guid orderId)
    {
        // Arrange
        var existingPackage = new Package
        {
            OrderId = orderId,
            Status = PackageStatus.Preparing,
            CustomerName = "Jane",
            CustomerEmail = "jane@example.com"
        };
        var request = new MarkOrderReadyRequest { CustomerName = "Jane", CustomerEmail = "jane@example.com" };

        _repository.Setup(r => r.GetByOrderIdAsync(orderId, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(existingPackage);
        _repository.Setup(r => r.CreateOrUpdateAsync(It.IsAny<Package>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync((Package p, CancellationToken _) => p);
        _publishEndpoint.Setup(p => p.Publish<OrderReadyForPickupEvent>(It.IsAny<object>(), It.IsAny<CancellationToken>()))
                        .Returns(Task.CompletedTask);

        // Act
        var result = await CreateSut().MarkReadyForPickupAsync(orderId, request, CancellationToken.None);

        // Assert: package status updated
        result.Status.Should().Be(PackageStatus.ReadyForPickup);

        // Assert: OrderReadyForPickupEvent published
        _publishEndpoint.Verify(p => p.Publish<OrderReadyForPickupEvent>(
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task ReportProblemAsync_UpdatesPackageAndPublishesPackageIssueReportedEvent(Guid orderId)
    {
        // Arrange
        var existingPackage = new Package { OrderId = orderId };
        var request = new ReportPackageProblemRequest
        {
            IssueType = "Damaged",
            Details = "Box torn",
            ReportedBy = "warehouse-01"
        };

        _repository.Setup(r => r.GetByOrderIdAsync(orderId, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(existingPackage);
        _repository.Setup(r => r.CreateOrUpdateAsync(It.IsAny<Package>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync((Package p, CancellationToken _) => p);
        _publishEndpoint.Setup(p => p.Publish<PackageIssueReportedEvent>(It.IsAny<object>(), It.IsAny<CancellationToken>()))
                        .Returns(Task.CompletedTask);

        // Act
        var result = await CreateSut().ReportProblemAsync(orderId, request, CancellationToken.None);

        // Assert: status and fields updated
        result.Status.Should().Be(PackageStatus.Failed);
        result.IssueType.Should().Be("Damaged");

        // Assert: PackageIssueReportedEvent published
        _publishEndpoint.Verify(p => p.Publish<PackageIssueReportedEvent>(
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
