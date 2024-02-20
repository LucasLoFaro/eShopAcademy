using Application.Managers;
using Domain.DTOs;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/stock")]
    public class StockController : ControllerBase
    {
        private readonly IStockManager _stockManager;

        public StockController(IStockManager stockManager)
        {
            _stockManager = stockManager;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<Stock>>> GetAll()
        {
            return Ok(await _stockManager.GetAllAsync());
        }


        [HttpGet("{productGuid}", Name = "GetStockByProductGuid")]
        public async Task<ActionResult<IReadOnlyList<Domain.Entities.Stock>>> GetStockByProductGuid(string productGuid)
        {
            IReadOnlyList<Domain.Entities.Stock> stockAvailable = await _stockManager.GetByProductGuidAsync(productGuid);
            
            if (stockAvailable == null) return NotFound("No stock available in warehouses");

            return Ok(stockAvailable);
        }


        [HttpGet("{productGuid}/warehouse/{warehouse}", Name = "GetStockByProductGuidAndWarehouse")]
        public async Task<ActionResult<Domain.Entities.Stock>> GetStockByProductGuidAndWarehouse(string productGuid, string warehouse)
        {
            var stock = await _stockManager.GetByProductGuidAndWarehouseAsync(productGuid, warehouse);

            if (stock == null) return NotFound($"No stock available in warehouse {warehouse}");
            
            return Ok(stock);
        }


        [HttpPost("Increase")]
        public async Task<ActionResult> IncreaseStock([FromForm] AlterStockDTO alterStock)
        {
            try
            {
                var stock = await _stockManager.IncreaseStock(alterStock);

                return new CreatedAtRouteResult($"GetStockByProductGuidAndWarehouse"
                    , new { productGuid = stock.ProductGuid, warehouse = stock.Warehouse }
                    , stock);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpPost("Decrease")]
        public async Task<ActionResult> DecreaseStock([FromForm] AlterStockDTO alterStock)
        {
            try
            {
                var stock = await _stockManager.DecreaseStock(alterStock);

                return new CreatedAtRouteResult($"GetStockByProductGuidAndWarehouse"
                    , new { productGuid = stock.ProductGuid, warehouse = stock.Warehouse }
                    , stock);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            { 
                throw; 
            }
        }

    }
}
