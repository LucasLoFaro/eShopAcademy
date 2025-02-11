using Infrastructure.Data;
using Core.Domain.DTOs;
using Core.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Infrastructure.Services.Interfaces;

namespace API.Controllers
{
    [ApiController]
    [Route("api/stock")]
    public class StockController : ControllerBase
    {
        public IStockRepository _stockRepository { get; }
        private readonly IMessagingServiceClient _messaging;

        public StockController(IStockRepository stockRepository, IMessagingServiceClient messagingServiceClient)
        {
            _stockRepository = stockRepository;
            _messaging = messagingServiceClient;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<Stock>>> GetAll()
        {
            return Ok(await _stockRepository.GetAllAsync());
        }


        [HttpGet("{productGuid}", Name = "GetStockByProductGuid")]
        public async Task<ActionResult<IReadOnlyList<Stock>>> GetStockByProductGuid(Guid productGuid)
        {
            IEnumerable<Stock> stockAvailable = await _stockRepository.GetByProductGuidAsync(productGuid);
            
            if (stockAvailable == null) return NotFound("No stock available in warehouses");

            return Ok(stockAvailable);
        }


        [HttpGet("{productGuid}/warehouse/{warehouse}", Name = "GetStockByProductGuidAndWarehouse")]
        public async Task<ActionResult<Stock>> GetStockByProductGuidAndWarehouse(Guid productGuid, String warehouse)
        {
            Stock stock = await _stockRepository.GetByProductGuidAndWarehouseAsync(productGuid, warehouse);

            if (stock == null) return NotFound($"No stock available in warehouse {warehouse}");
            
            return Ok(stock);
        }


        [HttpPost("Increase")]
        public async Task<ActionResult> IncreaseStock([FromForm] AlterStockDTO alterStock)
        {
            if (alterStock.Quantity <= 0) return BadRequest("The Quantity field must be greater than zero");

            Stock stock = await _stockRepository.GetByProductGuidAndWarehouseAsync(alterStock.ProductGuid, alterStock.Warehouse);

            if (stock == null)
            {
                Stock stockToAdd = new Stock();
                stockToAdd.ProductID = alterStock.ProductGuid;
                stockToAdd.Quantity = alterStock.Quantity;
                stockToAdd.Warehouse = alterStock.Warehouse;

                stock = await _stockRepository.AddAsync(stockToAdd);
            }
            else
            {
                stock.Quantity += alterStock.Quantity;
                await _stockRepository.UpdateAsync(stock);
            }

            // Todo:Add automapper
            await _messaging.SendStockUpdate(new AlterStockDTO()
            {
                ProductGuid = stock.ProductID,
                Quantity = stock.Quantity,
                Warehouse = stock.Warehouse
            });
            return new CreatedAtRouteResult($"GetStockByProductGuidAndWarehouse"
                ,new { productGuid = stock.ProductID, warehouse = stock.Warehouse}
                , stock);
        }


        [HttpPost("Decrease")]
        public async Task<ActionResult> DecreaseStock([FromForm] AlterStockDTO alterStock)
        {
            if (alterStock.Quantity <= 0) return BadRequest("The Quantity field must be greater than zero");

            Stock stock = await _stockRepository.GetByProductGuidAndWarehouseAsync(alterStock.ProductGuid, alterStock.Warehouse);

            if (stock == null) return BadRequest("The required product does not exists in stock");
            if (stock.Quantity < alterStock.Quantity) return BadRequest("The required product does not have enough units");
            
            stock.Quantity -= alterStock.Quantity;
            await _stockRepository.UpdateAsync(stock);

            // Todo:Add automapper
            await _messaging.SendStockUpdate(new AlterStockDTO()
            {
                ProductGuid = stock.ProductID,
                Quantity = stock.Quantity,
                Warehouse = stock.Warehouse
            });

            return new CreatedAtRouteResult($"GetStockByProductGuidAndWarehouse"
                , new { productGuid = stock.ProductID, warehouse = stock.Warehouse }
                , stock);
        }

    }
}
