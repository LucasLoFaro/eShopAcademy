using Data;
using Domain.DTOs;
using Domain.Entities;
using Google.Protobuf.WellKnownTypes;
using gRPC.Protos;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc;

namespace gRPC.Services
{
    public class StockService: StockProtoService.StockProtoServiceBase
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
            Stock stock = await _stockRepository.GetByProductGuidAndWarehouseAsync(request.ProductGuid, request.Warehouse);

            if (stock == null)
                throw new RpcException(new Status(StatusCode.NotFound, $"Stock for Product with Guid={request.ProductGuid} was not found"));

            var response = new StockModel()
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
            if (request.Quantity <= 0) throw new RpcException(new Status(StatusCode.InvalidArgument, "The Quantity field must be greater than zero"));

            Stock stockAvailable = await _stockRepository.GetByProductGuidAndWarehouseAsync(request.ProductGuid, request.Warehouse);

            if (stockAvailable == null)
            {
                Stock stockToAdd = new Stock();
                stockToAdd.ProductGuid = request.ProductGuid;
                stockToAdd.Quantity = request.Quantity;
                stockToAdd.Warehouse = request.Warehouse;

                await _stockRepository.AddAsync(stockToAdd);
            }
            else
            {
                stockAvailable.Quantity += request.Quantity;
                await _stockRepository.UpdateAsync(stockAvailable);
            }
            return new IncreaseStockResponse() { Success = true };
        }


        public override async Task<DecreaseStockResponse> DecreaseStock(DecreaseStockRequest request, ServerCallContext context)
        {
            if (request.Quantity <= 0) throw new RpcException(new Status(StatusCode.InvalidArgument, "The Quantity field must be greater than zero"));

            Stock stockAvailable = await _stockRepository.GetByProductGuidAndWarehouseAsync(request.ProductGuid, request.Warehouse);

            if (stockAvailable == null)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "The required product does not exists in stock"));
            if (stockAvailable.Quantity < request.Quantity)
                throw new RpcException(new Status(StatusCode.FailedPrecondition, "The required product does not have enough units"));

            stockAvailable.Quantity -= request.Quantity;
            await _stockRepository.UpdateAsync(stockAvailable);

            return new DecreaseStockResponse() { Success = true };
        }


        public override Task<Empty> Test(Empty request, ServerCallContext context)
        {
            return Task.FromResult(new Empty());
        }


    }
}
