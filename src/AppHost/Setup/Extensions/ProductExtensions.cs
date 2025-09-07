namespace AppHost.Setup.Extensions;

public static class ProductExtensions
{
    public static void Configure(
        IResourceBuilder<ProjectResource> productApi,
        IResourceBuilder<ProjectResource> productGrpc,
        IResourceBuilder<ContainerResource> cassandra,
        IResourceBuilder<RabbitMQServerResource> rabbit)

    {
        productApi
            .WithReference(rabbit)
            .WithCassandra(cassandra)
            .WithCommonEnvironments()
            .WithEndpoint(port: 8001, targetPort: 8080, name: "http")
            .WithEnvironment("AZURE_CLIENT_ID", "facc151c-6753-4101-9035-e10cc34a38f3")
            .WithEnvironment("AZURE_CLIENT_SECRET", "Dsq8Q~D6TAdUK9VBKYM.M67LpDyZ_kS210.33cE3");

        productGrpc
            .WithReference(rabbit)
            .WithCassandra(cassandra)
            .WithCommonEnvironments()
            .WithEndpoint(port: 8021, targetPort: 8080, name: "grpc")
            .WithEnvironment("AZURE_CLIENT_ID", "facc151c-6753-4101-9035-e10cc34a38f3")
            .WithEnvironment("AZURE_CLIENT_SECRET", "Dsq8Q~D6TAdUK9VBKYM.M67LpDyZ_kS210.33cE3");
    }
}