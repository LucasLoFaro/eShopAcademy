using AppHost.Setup.Resources;
using Aspire.Hosting;

namespace AppHost.Setup.Extensions;

public static class ProductsExtensions
{
    public static void Configure(
        IResourceBuilder<ProjectResource> productApi,
        IResourceBuilder<ProjectResource> productGrpc,
        IResourceBuilder<CosmosDbResource> cosmosdb,
        IResourceBuilder<RabbitMQServerResource> rabbit)

    {
        productApi
            .WithReference(rabbit)
            .WaitFor(rabbit)
            .WithReference(cosmosdb)
            .WaitFor(cosmosdb)
            .WithCommonEnvironments()
            .WithHttpEndpoint(port: 8001, name: "products-api")
            .WithEnvironment("AZURE_CLIENT_ID", "facc151c-6753-4101-9035-e10cc34a38f3")
            .WithEnvironment("AZURE_CLIENT_SECRET", "Dsq8Q~D6TAdUK9VBKYM.M67LpDyZ_kS210.33cE3");

        productGrpc
            .WithReference(rabbit)
            .WaitFor(rabbit)
            .WithReference(cosmosdb)
            .WaitFor(cosmosdb)
            .WithCommonEnvironments()
            .WithHttpEndpoint(port: 8021, name: "products-grpc")
            .WithEnvironment("AZURE_CLIENT_ID", "facc151c-6753-4101-9035-e10cc34a38f3")
            .WithEnvironment("AZURE_CLIENT_SECRET", "Dsq8Q~D6TAdUK9VBKYM.M67LpDyZ_kS210.33cE3");
    }
}