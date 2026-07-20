using MassTransit;

namespace NotificationService;

public sealed class OrderNotificationConsumerDefinition : NotificationConsumerDefinition<OrderNotificationConsumer>
{
    public OrderNotificationConsumerDefinition() : base("order-notifications") { }
}

public sealed class SellerNotificationConsumerDefinition : NotificationConsumerDefinition<SellerNotificationConsumer>
{
    public SellerNotificationConsumerDefinition() : base("seller-notifications") { }
}

public abstract class NotificationConsumerDefinition<TConsumer> : ConsumerDefinition<TConsumer>
    where TConsumer : class, IConsumer
{
    protected NotificationConsumerDefinition(string endpointName)
    {
        EndpointName = endpointName;
        ConcurrentMessageLimit = 8;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<TConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(retry =>
        {
            retry.Handle<MongoDB.Driver.MongoException>();
            retry.Handle<HttpRequestException>();
            retry.Handle<TimeoutException>();
            retry.Intervals(TimeSpan.FromMilliseconds(200), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3));
        });
        endpointConfigurator.UseMongoDbOutbox(context);
    }
}
