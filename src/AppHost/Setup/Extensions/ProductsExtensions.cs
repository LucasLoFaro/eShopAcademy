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
            .WithEnvironment("ContentSafety__Endpoint", "https://eshopacademy-contentsafety.cognitiveservices.azure.com/");

                    productGrpc
            .WithReference(rabbit)
            .WaitFor(rabbit)
            .WithReference(cosmosdb)
            .WaitFor(cosmosdb)
            .WithCommonEnvironments()
            .WithHttpEndpoint(port: 8021, name: "products-grpc")
            .WithEnvironment("ContentSafety__Endpoint", "https://eshopacademy-contentsafety.cognitiveservices.azure.com/");
    }
}