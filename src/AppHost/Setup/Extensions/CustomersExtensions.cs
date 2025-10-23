namespace AppHost.Setup.Extensions;

public static class CustomersExtensions
{
    public static void Configure(
        IResourceBuilder<ProjectResource> customerApi,
        IResourceBuilder<MongoDBDatabaseResource> customersdb,
        IResourceBuilder<RabbitMQServerResource> rabbit)
    {
        customerApi
            .WithReference(customersdb)
            .WaitFor(customersdb)
            .WithReference(rabbit)
            .WaitFor(rabbit)
            .WithCommonEnvironments()
            .WithHttpEndpoint(port: 8008, name: "customers-api")
            .WithEnvironment("AZURE_CLIENT_ID", "f8414e0b-f3fc-417e-9579-dcf2522d012f")
            .WithEnvironment("AZURE_CLIENT_SECRET", "5ou8Q~aiomsSKzKOQo89Eg6O4uKInhC2rM3fncCW");
    }
}