namespace AppHost.Setup.Extensions;

public static class PaymentExtensions
{
    public static void Configure(
        IResourceBuilder<ProjectResource> paymentApi,
        IResourceBuilder<ProjectResource> paymentGrpc,
        IResourceBuilder<ContainerResource> wiremock,
        IResourceBuilder<RabbitMQServerResource> rabbit)
    {
        paymentApi
            .WaitFor(wiremock)
            .WithReference(rabbit)
            .WaitFor(rabbit)
            .WithCommonEnvironments()
            .WithHttpEndpoint(port: 8006, name: "payment-api")
            .WithEnvironment("Payment__SignatureSecret", "Sup3rSecr3t!")
            .WithEnvironment("AZURE_CLIENT_ID", "4ac9deea-ea6d-4794-8414-dfae912b50a2")
            .WithEnvironment("AZURE_CLIENT_SECRET", "gM88Q~-WrvT.yyZA8exq~kC6ydUTQqBZWDPioaEx");

        paymentGrpc
            .WaitFor(wiremock)
            .WithReference(rabbit)
            .WaitFor(rabbit)
            .WithCommonEnvironments()
            .WithHttpEndpoint(port: 8026, name: "payment-grpc")
            .WithEnvironment("WireMock__BaseUrl", "http://wiremock:8080")
            .WithEnvironment("Payment__SignatureSecret", "Sup3rSecr3t!")
            .WithEnvironment("AZURE_CLIENT_ID", "4ac9deea-ea6d-4794-8414-dfae912b50a2")
            .WithEnvironment("AZURE_CLIENT_SECRET", "gM88Q~-WrvT.yyZA8exq~kC6ydUTQqBZWDPioaEx");
    }
}
