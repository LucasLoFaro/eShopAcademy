using Aspire.Hosting;

namespace AppHost.Setup.Extensions;

public static class ShippingExtensions
{
    public static void Configure(
        IResourceBuilder<ProjectResource> shippingApi,
        IResourceBuilder<ProjectResource> shippingService,
        IResourceBuilder<ProjectResource> shippingSimulator,
        IResourceBuilder<RabbitMQServerResource> rabbit,
        IResourceBuilder<MongoDBDatabaseResource> shippingdb,
        IResourceBuilder<RedisResource> redis)
    {
        shippingSimulator
            .WithReference(shippingApi)
            .WithReference(redis)
            .WaitFor(redis)
            .WithCommonEnvironments()
            .WithHttpEndpoint(port: 8027, name: "shipping-simulator")
            .WithEnvironment("ShippingSimulator__RedisDatabase", "1");

        shippingApi
            .WithReference(shippingdb)
            .WaitFor(shippingdb)
            .WaitFor(shippingSimulator)
            .WithReference(shippingSimulator)
            .WithReference(rabbit)
            .WaitFor(rabbit)
            .WithCommonEnvironments()
            .WithHttpEndpoint(port: 8007, name: "shipping-api")
            .WithEnvironment("Shipping__SignatureSecret", "Sup3rSecr3t!")
            .WithEnvironment("Shipping__Provider__BaseUrl", "http://_shipping-simulator.eshopacademy-shipping-simulator");

        shippingService
            .WithReference(shippingdb)
            .WaitFor(shippingdb)
            .WaitFor(shippingSimulator)
            .WithReference(shippingSimulator)
            .WithReference(rabbit)
            .WaitFor(rabbit)
            .WithCommonEnvironments()
            .WithEnvironment("Shipping__SignatureSecret", "Sup3rSecr3t!")
            .WithEnvironment("Shipping__Provider__BaseUrl", "http://_shipping-simulator.eshopacademy-shipping-simulator");
    }
}
