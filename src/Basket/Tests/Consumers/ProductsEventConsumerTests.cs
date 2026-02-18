using AutoFixture.Xunit2;
using AutoMapper;
using Basket.EventsProcessor.Consumers;
using Data.Interfaces;
using Domain.Basket.Contracts;
using Domain.Common.Events.Products;
using EventsProcessor;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Basket.Tests.Consumers;

public class ProductsEventConsumerTests
{
    private static IMapper CreateMapper()
        => new MapperConfiguration(cfg => cfg.AddProfile<EventMapper>(), NullLoggerFactory.Instance).CreateMapper();

    [Theory]
    [AutoData]
    public async Task Consume_CallsAddOrUpdateProductWithMappedDto(ProductUpdatedEvent evt)
    {
        // Arrange
        var productCache = new Mock<IProductCache>();
        productCache.Setup(c => c.AddOrUpdateProduct(It.IsAny<ProductDTO>())).ReturnsAsync(true);

        var consumer = new ProductsEventConsumer(productCache.Object, CreateMapper());
        var context = new Mock<ConsumeContext<ProductUpdatedEvent>>();
        context.Setup(c => c.Message).Returns(evt);

        // Act
        await consumer.Consume(context.Object);

        // Assert: cache receives correctly mapped values
        productCache.Verify(c => c.AddOrUpdateProduct(
            It.Is<ProductDTO>(dto =>
                dto.ID == evt.ProductId &&
                dto.Name == evt.Name &&
                dto.Price == evt.Price)),
            Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task Consume_WhenCacheReturnsFalse_DoesNotThrow(ProductUpdatedEvent evt)
    {
        // Arrange
        var productCache = new Mock<IProductCache>();
        productCache.Setup(c => c.AddOrUpdateProduct(It.IsAny<ProductDTO>())).ReturnsAsync(false);

        var consumer = new ProductsEventConsumer(productCache.Object, CreateMapper());
        var context = new Mock<ConsumeContext<ProductUpdatedEvent>>();
        context.Setup(c => c.Message).Returns(evt);

        // Act
        var act = () => consumer.Consume(context.Object);

        // Assert
        await act.Should().NotThrowAsync();
    }
}
