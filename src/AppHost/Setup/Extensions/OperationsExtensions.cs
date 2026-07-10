namespace AppHost.Setup.Extensions;

public static class OperationsExtensions
{
    public static void Configure(
        IResourceBuilder<ProjectResource> operationApi, 
        IResourceBuilder<ProjectResource> operationService,
        IResourceBuilder<MongoDBDatabaseResource> operationsdb,
        IResourceBuilder<RabbitMQServerResource> rabbit)
    {
        operationApi
            .WithReference(operationsdb)
            .WaitFor(operationsdb)
            .WithReference(rabbit)
            .WaitFor(rabbit)
            .WithCommonEnvironments()
            .WithHttpEndpoint(port: 8009, name: "operations-api");

        operationService
            .WithReference(operationsdb)
            .WaitFor(operationsdb)
            .WithReference(rabbit)
            .WaitFor(rabbit)
            .WithCommonEnvironments();
    }
}