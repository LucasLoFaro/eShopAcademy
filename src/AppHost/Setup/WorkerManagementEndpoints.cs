using Aspire.Hosting;

namespace AppHost.Setup;

public static class WorkerManagementEndpoints
{
    public static void Configure(
        IResourceBuilder<ProjectResource> basketEvents,
        IResourceBuilder<ProjectResource> customersMessaging,
        IResourceBuilder<ProjectResource> sellersService,
        IResourceBuilder<ProjectResource> sellersEventsProcessor,
        IResourceBuilder<ProjectResource> stockMessaging,
        IResourceBuilder<ProjectResource> shippingService,
        IResourceBuilder<ProjectResource> operationsService,
        IResourceBuilder<ProjectResource> notificationService,
        IResourceBuilder<ProjectResource> ordersMessaging,
        IResourceBuilder<ProjectResource> ordersOrchestration,
        IResourceBuilder<ProjectResource> paymentsMessaging)
    {
        ConfigureEndpoint(basketEvents, "basket-events-management", 8101);
        ConfigureEndpoint(customersMessaging, "customers-messaging-management", 8102);
        ConfigureEndpoint(sellersService, "sellers-service-management", 8103);
        ConfigureEndpoint(sellersEventsProcessor, "sellers-events-management", 8104);
        ConfigureEndpoint(stockMessaging, "stock-messaging-management", 8105, useDedicatedHealthListener: true);
        ConfigureEndpoint(shippingService, "shipping-service-management", 8106);
        ConfigureEndpoint(operationsService, "operations-service-management", 8107);
        ConfigureEndpoint(notificationService, "notification-service-management", 8108);
        ConfigureEndpoint(ordersMessaging, "orders-messaging-management", 8109);
        ConfigureEndpoint(ordersOrchestration, "orchestration-management", 8110);
        ConfigureEndpoint(paymentsMessaging, "payments-messaging-management", 8111);
    }

    private static void ConfigureEndpoint(
        IResourceBuilder<ProjectResource> resource,
        string endpointName,
        int port,
        bool useDedicatedHealthListener = false)
    {
        resource
            .WithHttpEndpoint(
                port: port,
                targetPort: port,
                name: endpointName,
                isProxied: false)
            .WithHttpHealthCheck(() => resource.GetEndpoint(endpointName), "/health");

        var listenerUrl = $"http://0.0.0.0:{port}";
        resource.WithEnvironment(
            useDedicatedHealthListener ? "Management__Health__Url" : "ASPNETCORE_URLS",
            listenerUrl);
    }
}
