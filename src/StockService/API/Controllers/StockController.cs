using Data;
using Domain.DTOs;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using System.Collections.Generic;

namespace API.Controllers
{
    [ApiController]
    [Route("api/stock")]
    public class StockController : ControllerBase
    {
        public readonly StockDbContext _context;
        public IStockRepository _stockRepository { get; }

        public StockController(StockDbContext context, IStockRepository stockRepository)
        {
            _context = context;
            _stockRepository = stockRepository;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<Stock>>> GetAll()
        {
            return Ok(await _stockRepository.GetAllAsync());
        }


        [HttpGet("{productGuid}", Name = "GetStockByProductGuid")]
        public async Task<ActionResult<IReadOnlyList<Stock>>> GetStockByProductGuid(string productGuid)
        {
            IEnumerable<Stock> stockAvailable = await _stockRepository.GetByProductGuidAsync(productGuid);
            
            if (stockAvailable == null) return NotFound("No stock available in warehouses");

            return Ok(stockAvailable);
        }


        [HttpGet("{productGuid}/warehouse/{warehouse}", Name = "GetStockByProductGuidAndWarehouse")]
        public async Task<ActionResult<Stock>> GetStockByProductGuidAndWarehouse(string productGuid, string warehouse)
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

            return new CreatedAtRouteResult($"GetStockByProductGuidAndWarehouse"
                ,new { productGuid = stock.ProductGuid, warehouse = stock.Warehouse}
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

            return new CreatedAtRouteResult($"GetStockByProductGuidAndWarehouse"
                , new { productGuid = stock.ProductGuid, warehouse = stock.Warehouse }
                , stock);
        }



    }


}
