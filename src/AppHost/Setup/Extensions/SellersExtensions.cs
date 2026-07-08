namespace AppHost.Setup.Extensions;

public static class SellersExtensions
{
    public static void Configure(
        IResourceBuilder<ProjectResource> sellersApi,
        IResourceBuilder<ProjectResource> sellersService,
        IResourceBuilder<ProjectResource> sellersEventsProcessor,
        IResourceBuilder<MongoDBDatabaseResource> sellersdb,
        IResourceBuilder<RabbitMQServerResource> rabbit)
    {
        sellersApi
            .WithReference(sellersdb)
            .WaitFor(sellersdb)
            .WithReference(rabbit)
            .WaitFor(rabbit)
            .WithCommonEnvironments()
            .WithHttpEndpoint(port: 8010, name: "sellers-api")
            .WithEnvironment("SellersSpaOrigin", "http://localhost:5174,http://localhost:58010")
            .WithEnvironment("EntraId__Instance", "https://login.microsoftonline.com/")
            .WithEnvironment("EntraId__TenantId", "82abd0e4-97e7-4ad4-9b49-4c93188625ed")
            .WithEnvironment("EntraId__ClientId", "f54e2efa-6412-44d1-a342-34229767db5c")
            .WithEnvironment("EntraId__Audience", "api://f54e2efa-6412-44d1-a342-34229767db5c");

        sellersService
            .WithReference(sellersdb)
            .WaitFor(sellersdb)
            .WithReference(rabbit)
            .WaitFor(rabbit)
            .WithCommonEnvironments();

        sellersEventsProcessor
            .WithReference(sellersdb)
            .WaitFor(sellersdb)
            .WithReference(rabbit)
            .WaitFor(rabbit)
            .WithCommonEnvironments();
    }
}
