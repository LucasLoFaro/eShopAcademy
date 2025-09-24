namespace AppHost.Setup.Extensions;

public static class StockExtensions
{
    public static void Configure(
        IResourceBuilder<ProjectResource> stockApi,
        IResourceBuilder<ProjectResource> stockGrpc,
        IResourceBuilder<MongoDBDatabaseResource> stockdb,
        IResourceBuilder<RabbitMQServerResource> rabbit)
    {
        stockApi
            .WithReference(stockdb)
            .WaitFor(stockdb)
            .WithReference(rabbit)
            .WaitFor(rabbit)
            .WithCommonEnvironments()
            .WithHttpEndpoint(port: 8002, name: "stock-api")
            .WithEnvironment("AZURE_CLIENT_ID", "f8414e0b-f3fc-417e-9579-dcf2522d012f")
            .WithEnvironment("AZURE_CLIENT_SECRET", "5ou8Q~aiomsSKzKOQo89Eg6O4uKInhC2rM3fncCW");

        stockGrpc
            .WithReference(stockdb)
            .WaitFor(stockdb)
            .WithReference(rabbit)
            .WaitFor(rabbit)
            .WithCommonEnvironments()
            .WithHttpEndpoint(port: 8022, name: "stock-grpc")
            .WithEnvironment("AZURE_CLIENT_ID", "f8414e0b-f3fc-417e-9579-dcf2522d012f")
            .WithEnvironment("AZURE_CLIENT_SECRET", "5ou8Q~aiomsSKzKOQo89Eg6O4uKInhC2rM3fncCW");
    }
}