namespace AppHost.Setup.Extensions;

public static class ShippingExtensions
{
    public static void Configure(
        IResourceBuilder<ProjectResource> shippingApi,
        IResourceBuilder<ProjectResource> shippingService,
        IResourceBuilder<ContainerResource> wiremock,
        IResourceBuilder<RabbitMQServerResource> rabbit)
    {
        shippingApi
            .WaitFor(wiremock)
            .WithReference(rabbit)
            .WaitFor(rabbit)
            .WithCommonEnvironments()
            .WithHttpEndpoint(port: 8007, name: "shipping-api")
            .WithEnvironment("Shipping__SignatureSecret", "Sup3rSecr3t!")
            .WithEnvironment("AZURE_CLIENT_ID", "bdcf29bc-ebed-428f-9b86-e3d84d9a2314")
            .WithEnvironment("AZURE_CLIENT_SECRET", "ceL8Q~xXDZOCEjzSCB16Gv-cpDQxy7iKNcT6-cxX");

        shippingService
            .WaitFor(wiremock)
            .WithReference(rabbit)
            .WaitFor(rabbit)
            .WithCommonEnvironments()
            .WithEnvironment("WireMock__BaseUrl", "http://wiremock:8080")
            .WithEnvironment("Shipping__SignatureSecret", "Sup3rSecr3t!")
            .WithEnvironment("AZURE_CLIENT_ID", "bdcf29bc-ebed-428f-9b86-e3d84d9a2314")
            .WithEnvironment("AZURE_CLIENT_SECRET", "ceL8Q~xXDZOCEjzSCB16Gv-cpDQxy7iKNcT6-cxX");
    }
}
