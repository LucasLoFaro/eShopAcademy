using AutoFixture.Xunit2;
using Domain.Common.Commands.Shipping;
using Domain.Common.Events.Shipping;
using Domain.Shipping.Contracts.Responses;
using Domain.Shipping.Entities;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shipping.Application.Clients;
using Shipping.Application.Data;
using Shipping.Service.Consumers;
using Xunit;

namespace Shipping.Tests.Consumers;

public class ScheduleShippingCommandConsumerTests
{
    private readonly Mock<IShippingInfoRepository> _infoRepo = new();
    private readonly Mock<IShippingProviderClient> _providerClient = new();

    private ScheduleShippingCommandConsumer CreateSut() =>
        new(NullLogger<ScheduleShippingCommandConsumer>.Instance, _infoRepo.Object, _providerClient.Object);

    [Theory]
    [AutoData]
    public async Task Consume_WithCustomerEmail_UpsertInfoAndPublishesScheduledEvent(
        ScheduleShippingCommand command,
        ScheduleShippingResponse providerResponse)
    {
        // Arrange
        _infoRepo.Setup(r => r.UpsertAsync(It.IsAny<ShippingInfo>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _providerClient.Setup(p => p.ScheduleShippingAsync(It.IsAny<Domain.Shipping.Entities.Shipping>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(providerResponse);

        var context = new Mock<ConsumeContext<ScheduleShippingCommand>>();
        context.Setup(c => c.Message).Returns(command with { CustomerEmail = "test@example.com" });
        context.Setup(c => c.CancellationToken).Returns(CancellationToken.None);
        context.Setup(c => c.Publish(It.IsAny<ShippingScheduledEvent>(), It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);

        // Act
        await CreateSut().Consume(context.Object);

        // Assert: shipping info persisted
        _infoRepo.Verify(r => r.UpsertAsync(
            It.Is<ShippingInfo>(i => i.OrderId == command.OrderId && i.CustomerEmail == "test@example.com"),
            It.IsAny<CancellationToken>()), Times.Once);

        // Assert: ShippingScheduledEvent published with provider response values
        context.Verify(c => c.Publish(
            It.Is<ShippingScheduledEvent>(e =>
                e.OrderId == command.OrderId &&
                e.TrackingNumber == providerResponse.TrackingNumber &&
                e.Carrier == providerResponse.Carrier),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task Consume_WithoutCustomerEmail_SkipsUpsertAndStillPublishesScheduledEvent(
        ScheduleShippingCommand command,
        ScheduleShippingResponse providerResponse)
    {
        // Arrange
        _providerClient.Setup(p => p.ScheduleShippingAsync(It.IsAny<Domain.Shipping.Entities.Shipping>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(providerResponse);

        var context = new Mock<ConsumeContext<ScheduleShippingCommand>>();
        // Empty email means no info upsert
        context.Setup(c => c.Message).Returns(command with { CustomerEmail = string.Empty });
        context.Setup(c => c.CancellationToken).Returns(CancellationToken.None);
        context.Setup(c => c.Publish(It.IsAny<ShippingScheduledEvent>(), It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);

        // Act
        await CreateSut().Consume(context.Object);

        // Assert: no upsert when email is empty
        _infoRepo.Verify(r => r.UpsertAsync(It.IsAny<ShippingInfo>(), It.IsAny<CancellationToken>()), Times.Never);

        // Assert: event is still published
        context.Verify(c => c.Publish(It.IsAny<ShippingScheduledEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
