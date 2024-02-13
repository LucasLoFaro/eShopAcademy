using Infrastructure.Data;
using Domain.Entities;
using Google.Protobuf.WellKnownTypes;
using gRPC.Protos;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace gRPC.Services
{
    public class StockService : StockProtoService.StockProtoServiceBase
    {
        private readonly ILogger<StockService> _logger;
        private readonly IStockRepository _stockRepository;

        public StockService(ILogger<StockService> logger, IStockRepository stockRepository)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _stockRepository = stockRepository ?? throw new ArgumentNullException(nameof(stockRepository));
        }


        public override async Task GetAll(GetStockByProdGuidRequest request, IServerStreamWriter<StockModel> responseStream, ServerCallContext context)
        {
            IReadOnlyList<Stock> stockList = await _stockRepository.GetAllAsync();

            foreach (var stock in stockList)
            {
                var response = new StockModel()
                {
                    Id = stock._id.ToString(),
                    ProductGuid = stock.ProductID,
                    Quantity = stock.Quantity,
                    Warehouse = stock.Warehouse
                };
                //var productModel = _mapper.Map<ProductModel>(product);
                await responseStream.WriteAsync(response);
            }
        }


        public override async Task<StockModel> GetStockByProductGuidAndWarehouse(GetStockByProdAndWarehouseRequest request, ServerCallContext context)
        {
            Stock stock = await _stockRepository.GetByProductGuidAndWarehouseAsync(request.ProductGuid, request.Warehouse);

            if (stock == null)
                throw new RpcException(new Status(StatusCode.NotFound, $"Stock for Product with Guid={request.ProductGuid} was not found"));

            var response = new StockModel()
            {
                Id = stock._id.ToString(),
                ProductGuid = stock.ProductID,
                Quantity = stock.Quantity,
                Warehouse = stock.Warehouse
            };

            return response;
        }


        public override Task<Empty> Test(Empty request, ServerCallContext context)
        {
            return Task.FromResult(new Empty());
        }


    }
}
