using AppHost.Setup.Resources;

namespace AppHost.Setup.Extensions;

public static class PaymentsExtensions
{
    public static void Configure(
        IResourceBuilder<ProjectResource> paymentApi,
        IResourceBuilder<ProjectResource> paymentGrpc,
        IResourceBuilder<ProjectResource> paymentsMessaging,
        IResourceBuilder<WiremockResource> wiremock,
        IResourceBuilder<RabbitMQServerResource> rabbit)
    {
        paymentApi
            .WaitFor(rabbit)
            .WithReference(rabbit)
            .WithCommonEnvironments()
            .WithHttpEndpoint(port: 8006, name: "payments-api")
            .WithEnvironment("Payment__SignatureSecret", "Sup3rSecr3t!")
            .WithEnvironment("AZURE_CLIENT_ID", "4ac9deea-ea6d-4794-8414-dfae912b50a2")
            .WithEnvironment("AZURE_CLIENT_SECRET", "gM88Q~-WrvT.yyZA8exq~kC6ydUTQqBZWDPioaEx");

        paymentGrpc
            .WaitFor(wiremock)
            .WithReference(wiremock)
            .WithReference(rabbit)
            .WaitFor(rabbit)
            .WithCommonEnvironments()
            .WithHttpEndpoint(port: 8026, name: "payments-grpc")
            .WithEnvironment("WireMock__BaseUrl", "http://external-services-mocks:8080")
            .WithEnvironment("Payment__SignatureSecret", "Sup3rSecr3t!")
            .WithEnvironment("AZURE_CLIENT_ID", "4ac9deea-ea6d-4794-8414-dfae912b50a2")
            .WithEnvironment("AZURE_CLIENT_SECRET", "gM88Q~-WrvT.yyZA8exq~kC6ydUTQqBZWDPioaEx");

        paymentsMessaging
            .WaitFor(wiremock)
            .WithReference(wiremock)
            .WithReference(rabbit)
            .WaitFor(rabbit)
            .WithCommonEnvironments()
            .WithEnvironment("WireMock__BaseUrl", "http://external-services-mocks:8080")
            .WithEnvironment("Payment__SignatureSecret", "Sup3rSecr3t!")
            .WithEnvironment("AZURE_CLIENT_ID", "4ac9deea-ea6d-4794-8414-dfae912b50a2")
            .WithEnvironment("AZURE_CLIENT_SECRET", "gM88Q~-WrvT.yyZA8exq~kC6ydUTQqBZWDPioaEx");
    }
}
