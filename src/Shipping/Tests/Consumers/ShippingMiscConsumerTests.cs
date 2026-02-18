using AutoFixture.Xunit2;
using Domain.Common.Commands.Shipping;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shipping.Application.Clients;
using Shipping.Service.Consumers;
using Xunit;

namespace Shipping.Tests.Consumers;

public class CancelShippingCommandConsumerTests
{
    [Theory]
    [AutoData]
    public async Task Consume_CompletesWithoutThrowing(CancelShippingCommand command)
    {
        // Arrange
        var consumer = new CancelShippingCommandConsumer(NullLogger<CancelShippingCommandConsumer>.Instance);
        var context = new Mock<ConsumeContext<CancelShippingCommand>>();
        context.Setup(c => c.Message).Returns(command);

        // Act
        var act = () => consumer.Consume(context.Object);

        // Assert: fire-and-forget consumer; no exception expected
        await act.Should().NotThrowAsync();
    }
}

public class ConfirmPickupCommandConsumerTests
{
    [Theory]
    [AutoData]
    public async Task Consume_DelegatesToProviderClient(ConfirmPickupCommand command)
    {
        // Arrange
        var providerClient = new Mock<IShippingProviderClient>();
        providerClient.Setup(p => p.ConfirmPickupAsync(
                command.ShippingId, command.OrderId, command.ReadyAt, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var consumer = new ConfirmPickupCommandConsumer(
            providerClient.Object,
            NullLogger<ConfirmPickupCommandConsumer>.Instance);

        var context = new Mock<ConsumeContext<ConfirmPickupCommand>>();
        context.Setup(c => c.Message).Returns(command);
        context.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        // Act
        await consumer.Consume(context.Object);

        // Assert: provider client called with correct identifiers
        providerClient.Verify(p => p.ConfirmPickupAsync(
            command.ShippingId, command.OrderId, command.ReadyAt, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
