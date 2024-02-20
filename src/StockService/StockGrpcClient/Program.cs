using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
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

            Console.WriteLine("*********GetStockByProdAndWarehouseRequest()*********");
            GetStockByProdAndWarehouseRequest request = new GetStockByProdAndWarehouseRequest() { ProductGuid = "1234", Warehouse = "South" };
            StockModel response = await client.GetStockByProductGuidAndWarehouseAsync(request);
            
            Console.WriteLine($"Stock:\n" +
                $"_id: {response.Id}\n" +
                $"ProductGuid: {response.ProductGuid}\n" +
                $"Quantity: {response.Quantity}\n" +
                $"$Warehouse: {response.Warehouse}\n");

            Console.WriteLine("******************************");


            Console.WriteLine("\n\n*********GetAll()*********");
            GetStockByProdGuidRequest getStockRequest = new GetStockByProdGuidRequest() { ProductGuid = "1234" };
            using var clientData = client.GetAll(getStockRequest);
            await foreach (StockModel stockModel in clientData.ResponseStream.ReadAllAsync())
            {
                Console.WriteLine($"Stock:\n" +
                    $"_id: {stockModel.Id}\n" +
                    $"ProductGuid: {stockModel.ProductGuid}\n" +
                    $"Quantity: {stockModel.Quantity}\n" +
                    $"$Warehouse: {stockModel.Warehouse}\n");
            }
            Console.WriteLine("******************************");


            Console.WriteLine("\n\n*********IncreaseStockAsync*********");
            IncreaseStockRequest increaseStockRequest = new IncreaseStockRequest()
            {
                ProductGuid = "1234",
                Quantity = 10,
                Warehouse = "West"
            };
            IncreaseStockResponse increaseResponse = await client.IncreaseStockAsync(increaseStockRequest);
            Console.WriteLine($"Response: {increaseResponse}");
            Console.WriteLine("******************************");


            Console.WriteLine("\n\n*********IncreaseStockAsync*********");
            DecreaseStockRequest decreaseStockRequest = new DecreaseStockRequest()
            {
                ProductGuid = "1234",
                Quantity = 5,
                Warehouse = "West"
            };
            DecreaseStockResponse decreaseResponse = await client.DecreaseStockAsync(decreaseStockRequest);
            Console.WriteLine($"Response: {decreaseResponse}");
            Console.WriteLine("******************************");


            Console.WriteLine("\n\n*********Test()*********");
            //GetStockByProdGuidRequest getStockRequest = new GetStockByProdGuidRequest() { ProductGuid = "1234" };
            Empty testResponse = await client.TestAsync(new Empty());
            Console.WriteLine($"Response: {testResponse}");
            Console.WriteLine("******************************");

            Console.ReadKey();
        }
    }
}
