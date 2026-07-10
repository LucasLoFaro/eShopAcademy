namespace AppHost.Setup.Extensions;

public static class StockExtensions
{
    public static void Configure(
        IResourceBuilder<ProjectResource> stockApi,
        IResourceBuilder<ProjectResource> stockGrpc,
        IResourceBuilder<ProjectResource> stockMessaging,
        IResourceBuilder<MongoDBDatabaseResource> stockdb,
        IResourceBuilder<RabbitMQServerResource> rabbit)
    {
        stockApi
            .WithReference(stockdb)
            .WaitFor(stockdb)
            .WithReference(rabbit)
            .WaitFor(rabbit)
            .WithCommonEnvironments()
            .WithHttpEndpoint(port: 8002, name: "stock-api");

        stockGrpc
            .WithReference(stockdb)
            .WaitFor(stockdb)
            .WithReference(rabbit)
            .WaitFor(rabbit)
            .WithCommonEnvironments()
            .WithHttpEndpoint(port: 8022, name: "stock-grpc");

        stockMessaging
            .WithReference(stockdb)
            .WaitFor(stockdb)
            .WithReference(rabbit)
            .WaitFor(rabbit)
            .WithCommonEnvironments();
    }
}