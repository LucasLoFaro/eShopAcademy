using Application.IntegrationEvents.Messages;
using Domain.DTOs;
using Domain.Entities;
using MassTransit;
using StockMS.Data;

namespace Application.Managers
{
    //Contorller->Manager->Repository->Context
    public class StockManager : IStockManager
    {
        private readonly IStockRepository _stockRepository;
        private readonly IBus _bus;

        public StockManager(IStockRepository stockRepository, IBus bus)
        {
            _stockRepository = stockRepository;
            _bus = bus;
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

            StockChangedEvent stockChangedEvent = new(stock.ProductGuid, stock.Quantity, stock.Warehouse);
            await _bus.Publish(stockChangedEvent);

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

            StockChangedEvent stockIntegrationMessage = new StockChangedEvent(stock.ProductGuid, stock.Quantity, stock.Warehouse);
            await _bus.Publish(stockIntegrationMessage);

            return stock;
        }
    }
}
