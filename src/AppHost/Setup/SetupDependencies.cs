namespace AppHost.Setup;

public static class SetupDependencies
{
    public static void AddDevelopmentEnvironment(
        IDistributedApplicationBuilder builder,
        IResourceBuilder<ProjectResource> api)
    {
        // 🔹 WireMock
        var mappingsPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "Mocks"));
        var wiremock = builder.AddContainer("wiremock", "wiremock/wiremock:latest")
            .WithEndpoint(8080, targetPort: 8080)
            .WithBindMount(mappingsPath, "/home/wiremock/mappings");

        //api.WithReference(wiremock);

        // 🔹 RabbitMQ
        //var rabbit = builder.AddRabbitMQ("rabbit")
        //    .WithEndpoint(5672)
        //    .WithManagementPlugin(); // acceso en http://localhost:15672

        //api.WithReference(rabbit);

        // 🔹 Cassandra
        var cassandra = builder.AddContainer("cassandra", "cassandra", "latest")
            .WithEndpoint(port: 9042, targetPort: 9042, name: "cql")
            .WithEnvironment("CASSANDRA_CLUSTER_NAME", "TestCluster")
            .WithEnvironment("CASSANDRA_START_RPC", "true");


        //api.WithReference(cassandra);

    }
}
