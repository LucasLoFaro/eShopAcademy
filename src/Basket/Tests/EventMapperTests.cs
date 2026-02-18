using AutoMapper;
using Domain.Basket.Contracts;
using Domain.Common.Events.Products;
using Domain.Common.Events.Stock;
using EventsProcessor;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Basket.Tests;

public class EventMapperTests
{
    private readonly IMapper _mapper = new MapperConfiguration(
        cfg => cfg.AddProfile<EventMapper>(), NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public void Map_ProductUpdatedEvent_To_ProductDTO_MapsAllFields()
    {
        // Arrange
        var evt = new ProductUpdatedEvent
        {
            ProductId = Guid.NewGuid(),
            Name = "Test Product",
            Price = 49.99
        };

        // Act
        var dto = _mapper.Map<ProductDTO>(evt);

        // Assert
        Assert.Equal(evt.ProductId, dto.ID);
        Assert.Equal(evt.Name, dto.Name);
        Assert.Equal(evt.Price, dto.Price);
    }

    [Fact]
    public void Map_StockUpdatedEvent_To_AlterStockDTO_MapsAllFields()
    {
        // Arrange
        var evt = new StockUpdatedEvent
        {
            ProductId = Guid.NewGuid(),
            Quantity = 42
        };

        // Act
        var dto = _mapper.Map<AlterStockDTO>(evt);

        // Assert
        Assert.Equal(evt.ProductId, dto.ProductGuid);
        Assert.Equal(evt.Quantity, dto.Quantity);
    }
}
