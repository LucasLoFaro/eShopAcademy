using AutoFixture.Xunit2;
using Domain.Common.Commands.Operations;
using Domain.Operations.Entities;
using Domain.Operations.Enums;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Operations.Application.Repositories;
using Operations.Service.Consumers;
using Xunit;

namespace Operations.Tests.Consumers;

public class PreparePackageCommandConsumerTests
{
    [Theory]
    [AutoData]
    public async Task Consume_CreatesPackageWithCorrectFieldsInRepository(PreparePackageCommand command)
    {
        // Arrange
        var repository = new Mock<IPackageRepository>();
        repository.Setup(r => r.CreateOrUpdateAsync(It.IsAny<Package>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((Package p, CancellationToken _) => p);

        var consumer = new PreparePackageCommandConsumer(
            NullLogger<PreparePackageCommandConsumer>.Instance,
            repository.Object);

        var context = new Mock<ConsumeContext<PreparePackageCommand>>();
        context.Setup(c => c.Message).Returns(command);
        context.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        // Act
        await consumer.Consume(context.Object);

        // Assert: package created with correct orderId, reservationId and Pending status
        repository.Verify(r => r.CreateOrUpdateAsync(
            It.Is<Package>(p =>
                p.OrderId == command.OrderId &&
                p.ReservationId == command.ReservationId &&
                p.Status == PackageStatus.Pending),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
