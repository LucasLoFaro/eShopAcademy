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
            .WithHttpEndpoint(port: 8004, name: "redis");

        basketEvents
            .WithReference(redis)
            .WaitFor(redis)
            .WithReference(rabbit)
            .WaitFor(rabbit)
            .WithCommonEnvironments();
    }
}