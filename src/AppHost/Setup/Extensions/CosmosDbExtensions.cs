using AppHost.Setup.Resources;

namespace Aspire.Hosting;

public static class CosmosDbExtensions
{
    public static IResourceBuilder<CosmosDbResource> AddCosmosDb(
        this IDistributedApplicationBuilder builder, string name = "cosmosdb")
            => builder.AddResource(new CosmosDbResource(name))
                .WithImage("mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator", "vnext-preview")
                .WithEnvironment("AZURE_COSMOS_EMULATOR_PARTITION_COUNT", "1")
                .WithEnvironment("AZURE_COSMOS_EMULATOR_ENABLE_DATA_PERSISTENCE", "true")
                .WithEnvironment("COSMOSDB_DATABASE_NAME", "eShopAcademy")
                .WithEnvironment("ENABLE_EXPLORER", "true")
                .WithVolume("cosmosdb-data", "/data")
                .WithHttpEndpoint(targetPort: 8081,
                                  port: 8081,
                                  name: CosmosDbResource.EndpointName)
                .WithHttpEndpoint(targetPort: 10250,
                                  port: 10250,
                                  name: "cosmosdb-0")
                .WithHttpEndpoint(targetPort: 10251,
                                  port: 10251,
                                  name: "cosmosdb-1")
                .WithHttpEndpoint(targetPort: 10252,
                                  port: 10252,
                                  name: "cosmosdb-2")
                .WithHttpEndpoint(targetPort: 10253,
                                  port: 10253,
                                  name: "cosmosdb-3")
                .WithHttpEndpoint(targetPort: 10254,
                                  port: 10254,
                                  name: "cosmosdb-4")
                .WithHttpEndpoint(targetPort: 10255,
                                  port: 10255,
                                  name: "cosmosdb-5")
                .WithHttpEndpoint(targetPort: 1234,
                                  port: 1234,
                                  name: "cosmosdb-dataexplorer");
}