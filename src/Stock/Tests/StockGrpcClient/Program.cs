using Grpc.Core;
using Grpc.Net.Client;
using Protos;

namespace StockGrpcClient;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var serviceUrl = args.FirstOrDefault()
            ?? Environment.GetEnvironmentVariable("STOCK_GRPC_URL")
            ?? "http://localhost:8022";

        using var channel = GrpcChannel.ForAddress(serviceUrl);
        var client = new StockProtoService.StockProtoServiceClient(channel);

        Console.WriteLine("*********GetStockByProdAndWarehouseRequest()*********");
        var request = new GetStockByProdAndWarehouseRequest
        {
            ProductGuid = "1234",
            Warehouse = "South"
        };
        var response = await client.GetStockByProductGuidAndWarehouseAsync(request);

        Console.WriteLine(
            $"Stock:\n" +
            $"_id: {response.Id}\n" +
            $"ProductGuid: {response.ProductGuid}\n" +
            $"Quantity: {response.Quantity}\n" +
            $"Warehouse: {response.Warehouse}\n");

        Console.WriteLine("******************************");
        Console.WriteLine("\n\n*********GetAll()*********");

        var getStockRequest = new GetStockByProdGuidRequest { ProductGuid = "1234" };
        using var clientData = client.GetAll(getStockRequest);
        await foreach (var stockModel in clientData.ResponseStream.ReadAllAsync())
        {
            Console.WriteLine(
                $"Stock:\n" +
                $"_id: {stockModel.Id}\n" +
                $"ProductGuid: {stockModel.ProductGuid}\n" +
                $"Quantity: {stockModel.Quantity}\n" +
                $"Warehouse: {stockModel.Warehouse}\n");
        }

        Console.WriteLine("******************************");
        Console.ReadKey();
    }
}
