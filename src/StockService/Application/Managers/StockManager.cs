using Data;
using Domain.DTOs;
using Domain.Entities;

namespace Application.Managers
{

    public class StockManager : IStockManager
    {
        private readonly IStockRepository _stockRepository;

        public StockManager(IStockRepository stockRepository)
        {
            _stockRepository = stockRepository;
        }


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
    }
}
