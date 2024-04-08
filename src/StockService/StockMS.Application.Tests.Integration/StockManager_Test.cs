using Application.IntegrationEvents.Messages;
using Application.Managers;
using Domain.DTOs;
using Domain.Entities;
using MassTransit;
using Moq;
using StockMS.Data;

namespace StockMS.Application.Tests.Integration
{
    public class StockManager_Test
    {

        //[Theory]
        //[InlineData("hola",  2)]
        //Otra forma del inline es el ClassData: https://youtu.be/gENFOSxnAZ4?t=5074
        [Fact]
        public async void GetAllAsync_HasData_ReturnsListOk()
        {
            //Arrange
            Mock<IBus> mockBus = new Mock<IBus>();
            Mock<IStockRepository> mockRepository = new Mock<IStockRepository>();
            List<Stock> stockList = new List<Stock>() {
                new Stock() {ProductGuid = "123", Quantity = 1, Warehouse = "1" },
                new Stock() {ProductGuid = "456", Quantity = 2, Warehouse = "2" },
                new Stock() {ProductGuid = "789", Quantity = 3, Warehouse = "3" },
                new Stock() {ProductGuid = "10", Quantity = 4, Warehouse = "4" }
            };

            mockRepository.Setup(u =>  u.GetAllAsync()).ReturnsAsync(stockList.AsReadOnly());
            StockManager stockManager = new StockManager(mockRepository.Object, mockBus.Object);

            //Act
            var response = await stockManager.GetAllAsync();

            //Assert
            Assert.Equal(stockList, response);
        }

        [Fact]
        public async void IncreaseStock_AlreadyExistsStock_EndsOk()
        {
            //Arrange
            const int PREVIOUS_QUANTITY = 1;
            Mock<IBus> mockBus = new Mock<IBus>();
            Mock<IStockRepository> mockRepository = new Mock<IStockRepository>();
            Stock existingStock = new Stock() { ProductGuid = "123", Quantity = PREVIOUS_QUANTITY, Warehouse = "1" };
    
            //Reviso el repositorio, y existe stock
            mockRepository.Setup(u => u.GetByProductGuidAndWarehouseAsync(existingStock.ProductGuid, existingStock.Warehouse)).ReturnsAsync(existingStock);

            StockManager stockManager = new StockManager(mockRepository.Object, mockBus.Object);
            
            AlterStockDTO alterStock = new AlterStockDTO() { 
                ProductGuid = existingStock.ProductGuid,
                Quantity = 1,
                Warehouse = "1" 
            };

            //Act
            Stock response = await stockManager.IncreaseStock(alterStock);

            //Assert
            mockRepository.Verify(mock => mock.UpdateAsync(It.IsAny<Stock>()), Times.Once);
            mockBus.Verify(mock => mock.Publish(It.IsAny<StockChangedEvent>(), default), Times.Once());
            Assert.Equal(PREVIOUS_QUANTITY + 1, response.Quantity);
        }

        [Fact]
        public async void IncreaseStock_DoesNotExistsStock_EndsOk()
        {
            //Arrange
            Mock<IBus> mockBus = new Mock<IBus>();
            Mock<IStockRepository> mockRepository = new Mock<IStockRepository>();
            const string PRODUCT_GUID = "123";
            const int QUANTITY = 1;
            const string WAREHOUSE = "1";

            //Reviso el repositorio, y no existe stock
            mockRepository.Setup(u => u.GetByProductGuidAndWarehouseAsync(PRODUCT_GUID, WAREHOUSE)).ReturnsAsync((Stock)null);
            mockRepository.Setup(u => u.AddAsync(It.IsAny<Stock>())).ReturnsAsync(new Stock()
            {
                ProductGuid = PRODUCT_GUID,
                Quantity = QUANTITY,
                Warehouse = WAREHOUSE
            });

            StockManager stockManager = new StockManager(mockRepository.Object, mockBus.Object);

            AlterStockDTO alterStock = new AlterStockDTO()
            {
                ProductGuid = PRODUCT_GUID,
                Quantity = QUANTITY,
                Warehouse = WAREHOUSE
            };

            //Act
            Stock response = await stockManager.IncreaseStock(alterStock);

            //Assert
            mockRepository.Verify(mock => mock.AddAsync(It.IsAny<Stock>()), Times.Once);
            mockBus.Verify(mock => mock.Publish(It.IsAny<StockChangedEvent>(), default), Times.Once());
            Assert.Equal(alterStock.Quantity, response.Quantity);
        }


        [Fact]
        public async void IncreaseStock_QuantityLessEqualZero_ReturnsException()
        {
            //Arrange
            Mock<IBus> mockBus = new Mock<IBus>();
            Mock<IStockRepository> mockRepository = new Mock<IStockRepository>();
            AlterStockDTO alterStock = new AlterStockDTO()
            {
                ProductGuid = "123",
                Quantity = 0,
                Warehouse = "1"
            };

            //Act
            StockManager stockManager = new StockManager(mockRepository.Object, mockBus.Object);

            //Assert
            ArgumentOutOfRangeException exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => stockManager.IncreaseStock(alterStock));
            Assert.Contains("greater than zero", exception.Message);
        }

    }
        
}