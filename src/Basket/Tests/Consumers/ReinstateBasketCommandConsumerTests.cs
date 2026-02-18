using AutoFixture.Xunit2;
using Basket.EventsProcessor.Consumers;
using Data.Interfaces;
using Domain.Basket.Entities;
using Domain.Common.Commands.Basket;
using MassTransit;
using Moq;
using Xunit;

namespace Basket.Tests.Consumers;

public class ReinstateBasketCommandConsumerTests
{
    [Theory]
    [AutoData]
    public async Task Consume_CallsReinstateBasketWithMappedItems(ReinstateBasketCommand command)
    {
        // Arrange
        var basketCache = new Mock<IBasketCache>();
        basketCache
            .Setup(c => c.ReinstateBasket(command.ClientId, It.IsAny<IReadOnlyCollection<Item>>()))
            .ReturnsAsync(true);

        var consumer = new ReinstateBasketCommandConsumer(basketCache.Object);
        var context = new Mock<ConsumeContext<ReinstateBasketCommand>>();
        context.Setup(c => c.Message).Returns(command);

        // Act
        await consumer.Consume(context.Object);

        // Assert: cache was called once with correct clientId and mapped items
        basketCache.Verify(c => c.ReinstateBasket(
            command.ClientId,
            It.Is<IReadOnlyCollection<Item>>(items =>
                items.Count == command.Items.Count &&
                items.All(i => command.Items.Any(ci => ci.ProductID == i.ProductID && ci.Quantity == i.Quantity)))),
            Times.Once);
    }

    [Fact]
    public async Task Consume_WhenItemsListIsEmpty_CallsReinstateBasketWithEmptyCollection()
    {
        // Arrange
        var command = new ReinstateBasketCommand { ClientId = Guid.NewGuid(), Items = [] };
        var basketCache = new Mock<IBasketCache>();
        basketCache
            .Setup(c => c.ReinstateBasket(command.ClientId, It.IsAny<IReadOnlyCollection<Item>>()))
            .ReturnsAsync(true);

        var consumer = new ReinstateBasketCommandConsumer(basketCache.Object);
        var context = new Mock<ConsumeContext<ReinstateBasketCommand>>();
        context.Setup(c => c.Message).Returns(command);

        // Act
        await consumer.Consume(context.Object);

        // Assert
        basketCache.Verify(c => c.ReinstateBasket(
            command.ClientId,
            It.Is<IReadOnlyCollection<Item>>(items => items.Count == 0)),
            Times.Once);
    }
}
