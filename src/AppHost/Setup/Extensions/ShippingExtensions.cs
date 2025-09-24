namespace AppHost.Setup.Extensions;

public static class ShippingExtensions
{
    public static void Configure(
        IResourceBuilder<ProjectResource> shippingGrpc,
        IResourceBuilder<ContainerResource> wiremock)
    {
        shippingGrpc
            .WaitFor(wiremock)
            .WithCommonEnvironments()
            .WithHttpEndpoint(port: 8025, name: "shipping-grpc")
            .WithEnvironment("WireMock__BaseUrl", "http://wiremock:8080")
            .WithEnvironment("AZURE_CLIENT_ID", "bdcf29bc-ebed-428f-9b86-e3d84d9a2314")
            .WithEnvironment("AZURE_CLIENT_SECRET", "ceL8Q~xXDZOCEjzSCB16Gv-cpDQxy7iKNcT6-cxX");
    }
}
