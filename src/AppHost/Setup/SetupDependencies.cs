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

        // Cassandra has been moved into the AppHost so that it can be
        // referenced by multiple microservices alongside other backing
        // resources (Redis, MongoDB and PostgreSQL). Only the WireMock
        // external service mock is configured here for development.




    }
}
