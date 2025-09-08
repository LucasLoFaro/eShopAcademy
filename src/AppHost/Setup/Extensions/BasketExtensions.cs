namespace AppHost.Setup.Extensions;

public static class BasketExtensions
{
    public static void Configure(
        IResourceBuilder<ProjectResource> basketApi,
        IResourceBuilder<ProjectResource> basketEvents,
        IResourceBuilder<RedisResource> redis,
        IResourceBuilder<RabbitMQServerResource> rabbit)
    {
        basketApi
            .WithReference(redis)
            .WaitFor(redis)
            .WithReference(rabbit)
            .WaitFor(rabbit)
            .WithCommonEnvironments()
            .WithEndpoint(port: 8004, targetPort: 8080, name: "http")
            .WithEnvironment("AZURE_CLIENT_ID", "3bef33a0-5cd1-49b3-ab3b-2291145e3f2c")
            .WithEnvironment("AZURE_CLIENT_SECRET", "DQe8Q~K8gDgM3vHpokpgrW3lD8au~igTkZQCxape");

        basketEvents
            .WithReference(redis)
            .WaitFor(redis)
            .WithReference(rabbit)
            .WaitFor(rabbit)
            .WithCommonEnvironments()
            .WithEnvironment("AZURE_CLIENT_ID", "3bef33a0-5cd1-49b3-ab3b-2291145e3f2c")
            .WithEnvironment("AZURE_CLIENT_SECRET", "DQe8Q~K8gDgM3vHpokpgrW3lD8au~igTkZQCxape");
    }
}