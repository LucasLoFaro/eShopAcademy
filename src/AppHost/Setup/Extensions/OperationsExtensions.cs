namespace AppHost.Setup.Extensions;

public static class OperationsExtensions
{
    public static void Configure(
        IResourceBuilder<ProjectResource> operationApi, 
        IResourceBuilder<ProjectResource> operationService,
        IResourceBuilder<PostgresDatabaseResource> operationsdb,
        IResourceBuilder<RabbitMQServerResource> rabbit)
    {
        operationApi
            .WithReference(operationsdb)
            .WaitFor(operationsdb)
            .WithReference(rabbit)
            .WaitFor(rabbit)
            .WithCommonEnvironments()
            .WithHttpEndpoint(port: 8009, name: "operations-api")
            .WithEnvironment("AZURE_CLIENT_ID", "31234358-e1e6-40e9-82fe-363a97801e4e")
            .WithEnvironment("AZURE_CLIENT_SECRET", "piI8Q~iaVPv30ukYTzBXKUi4FTaxoxglI4.Buapp");

        operationService
            .WithReference(operationsdb)
            .WaitFor(operationsdb)
            .WithReference(rabbit)
            .WaitFor(rabbit)
            .WithCommonEnvironments()
            .WithEnvironment("AZURE_CLIENT_ID", "31234358-e1e6-40e9-82fe-363a97801e4e")
            .WithEnvironment("AZURE_CLIENT_SECRET", "piI8Q~iaVPv30ukYTzBXKUi4FTaxoxglI4.Buapp");
    }
}