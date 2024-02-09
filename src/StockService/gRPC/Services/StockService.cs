//using Data;
using gRPC.Protos;
using Grpc.Core;

namespace gRPC.Services
{
    public class StockService: StockProtoService.StockProtoServiceBase
    {
        private readonly ILogger<StockService> _logger;
        //private readonly StockRepository _stockRepository;

        public StockService(ILogger<StockService> logger /*, StockRepository stockRepository*/)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            //_stockRepository = stockRepository ?? throw new ArgumentNullException(nameof(stockRepository));
        }

        public override Task<StockModel> GetStockByProductGuidAndWarehouse(GetStockByProdAndWarehouseRequest request, ServerCallContext context)
        {

          //  var cosas = _stockRepository.GetAllAsync();

            var response = new StockModel()
            {
                Id = "EsUnID",
                ProductGuid = "Es un Prod GUID",
                Quantity = 24,
                Warehouse = "East"
            };

            return Task.FromResult(response);
            //return base.GetStockByProductGuidAndWarehouse(request, context);
        }
    }
}
