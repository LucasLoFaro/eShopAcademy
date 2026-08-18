using AutoFixture.Xunit2;
using Basket.EventsProcessor.Consumers;
using Data.Interfaces;
using Domain.Common.Commands.Basket;
using MassTransit;
using Moq;
using Xunit;

namespace Basket.Tests.Consumers;

public class EmptyBasketCommandConsumerTests
{
    [Theory]
    [AutoData]
    public async Task Consume_CallsEmptyBasketWithClientId(EmptyBasketCommand command)
    {
        // Arrange
        var basketCache = new Mock<IBasketCache>();
        basketCache.Setup(c => c.EmptyBasket(command.ClientId)).ReturnsAsync(true);
        var consumer = new EmptyBasketCommandConsumer(basketCache.Object);
        var context = new Mock<ConsumeContext<EmptyBasketCommand>>();
        context.Setup(c => c.Message).Returns(command);

        // Act
        await consumer.Consume(context.Object);

        // Assert
        basketCache.Verify(c => c.EmptyBasket(command.ClientId), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task Consume_WhenCacheReturnsFalse_DoesNotThrow(EmptyBasketCommand command)
    {
        // Arrange
        var basketCache = new Mock<IBasketCache>();
        basketCache.Setup(c => c.EmptyBasket(command.ClientId)).ReturnsAsync(false);
        var consumer = new EmptyBasketCommandConsumer(basketCache.Object);
        var context = new Mock<ConsumeContext<EmptyBasketCommand>>();
        context.Setup(c => c.Message).Returns(command);

        // Act
        var act = () => consumer.Consume(context.Object);

        // Assert: consumer swallows the result without throwing
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Consume_WhenTransientCacheFailureOccurs_PropagatesForRetry()
    {
        var command = new EmptyBasketCommand { ClientId = Guid.NewGuid() };
        var basketCache = new Mock<IBasketCache>();
        basketCache.Setup(c => c.EmptyBasket(command.ClientId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("Redis timeout"));
        var context = new Mock<ConsumeContext<EmptyBasketCommand>>();
        context.Setup(c => c.Message).Returns(command);

        var act = () => new EmptyBasketCommandConsumer(basketCache.Object).Consume(context.Object);

        await act.Should().ThrowAsync<TimeoutException>();
    }

    [Fact]
    public async Task Consume_WhenBusinessIdentifierIsMissing_ThrowsPermanentFailure()
    {
        var context = new Mock<ConsumeContext<EmptyBasketCommand>>();
        context.Setup(c => c.Message).Returns(new EmptyBasketCommand { ClientId = Guid.Empty });

        var act = () => new EmptyBasketCommandConsumer(Mock.Of<IBasketCache>()).Consume(context.Object);

        await act.Should().ThrowAsync<ArgumentException>();
    }
}
