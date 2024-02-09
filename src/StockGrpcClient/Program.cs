using Grpc.Net.Client;
using StockGrpcClient.Protos;

namespace StockGrpcClient
{
    internal class Program
    {
        static async Task Main(string[] args)
        {

            using var channel = GrpcChannel.ForAddress("https://localhost:7059");
            var client = new StockProtoService.StockProtoServiceClient(channel);

            GetStockByProdAndWarehouseRequest request = new GetStockByProdAndWarehouseRequest() { ProductGuid = "1234", Warehouse = "South" };
            StockModel response = await client.GetStockByProductGuidAndWarehouseAsync(request);
            
            Console.WriteLine($"Stock:\n" +
                $"_id: {response.Id}\n" +
                $"ProductGuid: {response.ProductGuid}\n" +
                $"Quantity: {response.Quantity}\n" +
                $"$Warehouse: {response.Warehouse}\n");
            Console.Out.WriteLine("Press any key to exit...");
            Console.ReadKey();

        }
    }
}
