using Domain.Stock.Contracts;
using Domain.Stock.Entities;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/stock")]
    public class StockController : ControllerBase
    {
        public IStockRepository _stockRepository { get; }
        private readonly StockMessagingClient _messaging;
        private readonly IStockOperationStore _operationStore;

        public StockController(
            IStockRepository stockRepository,
            StockMessagingClient messagingServiceClient,
            IStockOperationStore operationStore)
        {
            _stockRepository = stockRepository;
            _messaging = messagingServiceClient;
            _operationStore = operationStore;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<Stock>>> GetAll(CancellationToken cancellationToken)
            => Ok(await _stockRepository.GetAllAsync(cancellationToken));


        [HttpGet("{productGuid}", Name = "GetStockByProductGuid")]
        public async Task<ActionResult<IReadOnlyList<Stock>>> GetStockByProductGuid(Guid productGuid, CancellationToken cancellationToken)
        {
            IEnumerable<Stock> stockAvailable = await _stockRepository.GetByProductGuidAsync(productGuid, cancellationToken);
            
            if (stockAvailable == null) 
                return NotFound("No stock available in warehouses");

            return Ok(stockAvailable);
        }


        [HttpGet("{productGuid}/warehouse/{warehouse}", Name = "GetStockByProductGuidAndWarehouse")]
        public async Task<ActionResult<Stock>> GetStockByProductId(Guid productGuid, CancellationToken cancellationToken)
        {
            Stock stock = await _stockRepository.GetByProductIdAsync(productGuid, cancellationToken);

            if (stock == null) 
                return NotFound($"No stock available for product {productGuid}");
            
            return Ok(stock);
        }


        [HttpPost("Increase")]
        public async Task<ActionResult> IncreaseStock([FromForm] AlterStockRequest alterStock, CancellationToken cancellationToken)
        {
            if (alterStock.Quantity <= 0) 
                return BadRequest("The Quantity field must be greater than zero");

            if (!TryGetIdempotencyKey(out var idempotencyKey))
                return BadRequest("The Idempotency-Key header is required.");
            if (!await _operationStore.TryBeginAsync(
                    $"increase:{alterStock.ProductGuid:N}:{idempotencyKey}", cancellationToken))
                return Conflict("This stock operation has already been accepted.");

            Stock stock = await _stockRepository.GetByProductIdAsync(alterStock.ProductGuid, cancellationToken);

            if (stock == null)
            {
                Stock stockToAdd = new Stock();
                stockToAdd.ProductID = alterStock.ProductGuid;
                stockToAdd.Quantity = alterStock.Quantity;
                stockToAdd.Warehouse = alterStock.Warehouse;

                stock = await _stockRepository.AddOrUpdateAsync(stockToAdd, cancellationToken);
            }
            else
            {
                stock.Quantity += alterStock.Quantity;
                await _stockRepository.AddOrUpdateAsync(stock, cancellationToken);
            }

            // Todo:Add automapper
            await _messaging.SendStockUpdate(new AlterStockRequest()
            {
                ProductGuid = stock.ProductID,
                Quantity = stock.Quantity,
                Warehouse = stock.Warehouse
            }, cancellationToken);
            return new CreatedAtRouteResult($"GetStockByProductGuidAndWarehouse",
                new { productGuid = stock.ProductID, warehouse = stock.Warehouse},
                stock);
        }


        [HttpPost("Decrease")]
        public async Task<ActionResult> DecreaseStock([FromForm] AlterStockRequest alterStock, CancellationToken cancellationToken)
        {
            if (alterStock.Quantity <= 0) 
                return BadRequest("The Quantity field must be greater than zero");

            if (!TryGetIdempotencyKey(out var idempotencyKey))
                return BadRequest("The Idempotency-Key header is required.");
            Stock stock = await _stockRepository.GetByProductIdAsync(alterStock.ProductGuid, cancellationToken);

            if (stock == null) 
                return BadRequest("The required product does not exists in stock");

            if (stock.Quantity < alterStock.Quantity) 
                return BadRequest("The required product does not have enough units");

            if (!await _operationStore.TryBeginAsync(
                    $"decrease:{alterStock.ProductGuid:N}:{idempotencyKey}", cancellationToken))
                return Conflict("This stock operation has already been accepted.");
            
            stock.Quantity -= alterStock.Quantity;
            await _stockRepository.AddOrUpdateAsync(stock, cancellationToken);

            // Todo:Add automapper
            await _messaging.SendStockUpdate(new AlterStockRequest()
            {
                ProductGuid = stock.ProductID,
                Quantity = stock.Quantity,
                Warehouse = stock.Warehouse
            }, cancellationToken);

            return new CreatedAtRouteResult($"GetStockByProductGuidAndWarehouse",
                new { productGuid = stock.ProductID, warehouse = stock.Warehouse },
                stock);
        }

        private bool TryGetIdempotencyKey(out string idempotencyKey)
        {
            idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault() ?? string.Empty;
            return idempotencyKey.Length is > 0 and <= 128;
        }

    }
}
