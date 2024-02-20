using Application.Managers;
using Data;

namespace Application_UnitTest
{
    public class Tests: TestHelper
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void GetAllAsync_Ok()
        {
            //Arrange
            string dbName = Guid.NewGuid().ToString();
            StockDbContext context = ContextFactory(dbName);
            StockRepository repository = new StockRepository(context);
            StockManager stockManager = new StockManager(repository);

            Assert.Pass();
        }


        [Test]
        public void GetByProductGuidAsync_Ok()
        {
            Assert.Pass();
        }


        [Test]
        public void GetByProductGuidAndWarehouseAsync_Ok()
        {
            Assert.Pass();
        }


        [Test]
        public void IncreaseStock_Ok()
        {
            Assert.Pass();
        }


        [Test]
        public void DecreaseStock_Ok()
        {
            Assert.Pass();
        }
        /*

        public async Task<IReadOnlyList<Stock>> GetAllAsync()
        {
            return await _stockRepository.GetAllAsync();
        }


        public async Task<IReadOnlyList<Stock>> GetByProductGuidAsync(string productGuid)
        {
            return await _stockRepository.GetByProductGuidAsync(productGuid);
        }


        public async Task<Stock> GetByProductGuidAndWarehouseAsync(string productGuid, string warehouse)
        {
            return await _stockRepository.GetByProductGuidAndWarehouseAsync(productGuid, warehouse);
        }


        public async Task<Stock> IncreaseStock(AlterStockDTO alterStock)
        {
            if (alterStock.Quantity <= 0)
                throw new ArgumentOutOfRangeException("The Quantity field must be greater than zero");

            Stock stock = await _stockRepository.GetByProductGuidAndWarehouseAsync(alterStock.ProductGuid, alterStock.Warehouse);

            if (stock == null)
            {
                Stock stockToAdd = new Stock();
                stockToAdd.ProductGuid = alterStock.ProductGuid;
                stockToAdd.Quantity = alterStock.Quantity;
                stockToAdd.Warehouse = alterStock.Warehouse;

                stock = await _stockRepository.AddAsync(stockToAdd);
            }
            else
            {
                stock.Quantity += alterStock.Quantity;
                await _stockRepository.UpdateAsync(stock);
            }

            return stock;
        }


        public async Task<Stock> DecreaseStock(AlterStockDTO alterStock)
        {
            if (alterStock.Quantity <= 0)
                throw new ArgumentOutOfRangeException("The Quantity field must be greater than zero");

            Stock stock = await _stockRepository.GetByProductGuidAndWarehouseAsync(alterStock.ProductGuid, alterStock.Warehouse);

            if (stock == null)
                throw new ArgumentException("The required product does not exists in stock");

            if (stock.Quantity < alterStock.Quantity)
                throw new ArgumentOutOfRangeException("The required product does not have enough units");

            stock.Quantity -= alterStock.Quantity;
            await _stockRepository.UpdateAsync(stock);

            return stock;
        }

        */
    }
}