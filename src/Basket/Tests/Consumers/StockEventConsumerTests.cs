using AutoFixture.Xunit2;
using AutoMapper;
using Basket.EventsProcessor.Consumers;
using Data.Interfaces;
using Domain.Basket.Contracts;
using Domain.Common.Events.Stock;
using EventsProcessor;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Basket.Tests.Consumers;

public class StockEventConsumerTests
{
    private static IMapper CreateMapper()
        => new MapperConfiguration(cfg => cfg.AddProfile<EventMapper>(), NullLoggerFactory.Instance).CreateMapper();

    [Theory]
    [AutoData]
    public async Task Consume_CallsUpdateProductStockWithMappedDto(StockUpdatedEvent evt)
    {
        // Arrange
        var productCache = new Mock<IProductCache>();
        productCache.Setup(c => c.UpdateProductStock(It.IsAny<AlterStockDTO>())).ReturnsAsync(true);

        var consumer = new StockEventConsumer(productCache.Object, CreateMapper());
        var context = new Mock<ConsumeContext<StockUpdatedEvent>>();
        context.Setup(c => c.Message).Returns(evt);

        // Act
        await consumer.Consume(context.Object);

        // Assert: cache receives correctly mapped values
        productCache.Verify(c => c.UpdateProductStock(
            It.Is<AlterStockDTO>(dto =>
                dto.ProductGuid == evt.ProductId &&
                dto.Quantity == evt.Quantity)),
            Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task Consume_WhenCacheReturnsFalse_DoesNotThrow(StockUpdatedEvent evt)
    {
        // Arrange
        var productCache = new Mock<IProductCache>();
        productCache.Setup(c => c.UpdateProductStock(It.IsAny<AlterStockDTO>())).ReturnsAsync(false);

        var consumer = new StockEventConsumer(productCache.Object, CreateMapper());
        var context = new Mock<ConsumeContext<StockUpdatedEvent>>();
        context.Setup(c => c.Message).Returns(evt);

        // Act
        var act = () => consumer.Consume(context.Object);

        // Assert
        await act.Should().NotThrowAsync();
    }
}
