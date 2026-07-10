using AppHost.Setup.Resources;

namespace AppHost.Setup.Extensions;

public static class PaymentsExtensions
{
    public static void Configure(
        IResourceBuilder<ProjectResource> paymentApi,
        IResourceBuilder<ProjectResource> paymentGrpc,
        IResourceBuilder<ProjectResource> paymentsMessaging,
        IResourceBuilder<ProjectResource> pspSimulator,
        IResourceBuilder<RabbitMQServerResource> rabbit)
    {
        pspSimulator
            .WithReference(paymentApi)
            .WithHttpEndpoint(port: 8050, name: "psp-simulator");

        paymentApi
            .WaitFor(rabbit)
            .WithReference(rabbit)
            .WithCommonEnvironments()
            .WithHttpEndpoint(port: 8006, name: "payments-api")
            .WithEnvironment("Payment__SignatureSecret", "Sup3rSecr3t!");

        paymentGrpc
            .WaitFor(pspSimulator)
            .WithReference(pspSimulator)
            .WithReference(rabbit)
            .WaitFor(rabbit)
            .WithCommonEnvironments()
            .WithHttpEndpoint(port: 8026, name: "payments-grpc")
            .WithEnvironment("Payment__SignatureSecret", "Sup3rSecr3t!");

        paymentsMessaging
            .WaitFor(pspSimulator)
            .WithReference(pspSimulator)
            .WithReference(rabbit)
            .WaitFor(rabbit)
            .WithCommonEnvironments()
            .WithEnvironment("Payment__SignatureSecret", "Sup3rSecr3t!");
    }
}
