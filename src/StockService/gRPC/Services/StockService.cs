using Application.Managers;
using Domain.DTOs;
using Domain.Entities;
using Google.Protobuf.WellKnownTypes;
using gRPC.Protos;
using Grpc.Core;

namespace gRPC.Services
{
    public class StockService: StockProtoService.StockProtoServiceBase
    {
        private readonly ILogger<StockService> _logger;
        private readonly IStockManager _stockManager;

        public StockService(ILogger<StockService> logger, IStockManager stockManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _stockManager = stockManager ?? throw new ArgumentNullException(nameof(stockManager));
        }


        public override async Task GetAll(GetStockByProdGuidRequest request, IServerStreamWriter<StockModel> responseStream, ServerCallContext context)
        {
            IReadOnlyList<Stock> stockList = await _stockManager.GetAllAsync();

            foreach (var stock in stockList)
            {
                var response = new StockModel()
                {
                    Id = stock._id.ToString(),
                    ProductGuid = stock.ProductGuid,
                    Quantity = stock.Quantity,
                    Warehouse = stock.Warehouse
                };

                //var productModel = _mapper.Map<ProductModel>(product);
                await responseStream.WriteAsync(response);
            }
        }


        public override async Task<StockModel> GetStockByProductGuidAndWarehouse(GetStockByProdAndWarehouseRequest request, ServerCallContext context)
        {

            var stock = await _stockManager.GetByProductGuidAndWarehouseAsync(request.ProductGuid, request.Warehouse);

            if (stock == null)
                throw new RpcException(new Status(StatusCode.NotFound, $"Stock for Product with Guid={request.ProductGuid} was not found"));

            StockModel response = new StockModel()
            {
                Id = stock._id.ToString(),
                ProductGuid = stock.ProductGuid,
                Quantity = stock.Quantity,
                Warehouse = stock.Warehouse
            };

            return response;
        }


        public override async Task<IncreaseStockResponse> IncreaseStock(IncreaseStockRequest request, ServerCallContext context)
        {
            try
            {
                AlterStockDTO alterStock = new AlterStockDTO()
                {
                    ProductGuid = request.ProductGuid,
                    Quantity = request.Quantity,
                    Warehouse = request.Warehouse
                };

                var stockAvailable = await _stockManager.IncreaseStock(alterStock);
                return new IncreaseStockResponse() { Success = true };
            }
            catch (ArgumentException)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "The Quantity field must be greater than zero"));
            }
        }


        public override async Task<DecreaseStockResponse> DecreaseStock(DecreaseStockRequest request, ServerCallContext context)
        {
            try
            {
                AlterStockDTO alterStock = new AlterStockDTO()
                {
                    ProductGuid = request.ProductGuid,
                    Quantity = request.Quantity,
                    Warehouse = request.Warehouse
                };

                var stockAvailable = await _stockManager.DecreaseStock(alterStock);
                return new DecreaseStockResponse() { Success = true };
            }
            catch (ArgumentException)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "The Quantity field must be greater than zero"));
            }
        }


        public override Task<Empty> Test(Empty request, ServerCallContext context)
        {
            return Task.FromResult(new Empty());
        }

    }
}
