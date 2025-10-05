namespace AppHost.Setup.Extensions;

public static class ShippingExtensions
{
    public static void Configure(
        IResourceBuilder<ProjectResource> shippingApi,
        IResourceBuilder<ProjectResource> shippingGrpc,
        IResourceBuilder<ContainerResource> wiremock,
        IResourceBuilder<RabbitMQServerResource> rabbit)
    {
        shippingApi
            .WaitFor(wiremock)
            .WithReference(rabbit)
            .WaitFor(rabbit)
            .WithCommonEnvironments()
            .WithHttpEndpoint(port: 8005, name: "shipping-api")
            .WithEnvironment("Shipping__SignatureSecret", "Sup3rSecr3t!")
            .WithEnvironment("AZURE_CLIENT_ID", "bdcf29bc-ebed-428f-9b86-e3d84d9a2314")
            .WithEnvironment("AZURE_CLIENT_SECRET", "ceL8Q~xXDZOCEjzSCB16Gv-cpDQxy7iKNcT6-cxX");
        shippingGrpc
            .WaitFor(wiremock)
            .WithCommonEnvironments()
            .WithHttpEndpoint(port: 8025, name: "shipping-grpc")
            .WithEnvironment("WireMock__BaseUrl", "http://wiremock:8080")
            .WithEnvironment("Shipping__SignatureSecret", "Sup3rSecr3t!")
            .WithEnvironment("AZURE_CLIENT_ID", "bdcf29bc-ebed-428f-9b86-e3d84d9a2314")
            .WithEnvironment("AZURE_CLIENT_SECRET", "ceL8Q~xXDZOCEjzSCB16Gv-cpDQxy7iKNcT6-cxX");
    }
}
