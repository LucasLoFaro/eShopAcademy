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
            .WithHttpEndpoint(port: 8010, name: "sellers-api");

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
