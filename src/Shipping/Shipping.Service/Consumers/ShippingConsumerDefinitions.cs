using MassTransit;

namespace Shipping.Service.Consumers;

public sealed class ScheduleShippingCommandConsumerDefinition : ShippingConsumerDefinition<ScheduleShippingCommandConsumer>
{
    public ScheduleShippingCommandConsumerDefinition() : base("schedule-shipping") { }
}

public sealed class CancelShippingCommandConsumerDefinition : ShippingConsumerDefinition<CancelShippingCommandConsumer>
{
    public CancelShippingCommandConsumerDefinition() : base("cancel-shipping") { }
}

public sealed class OrderDeliveredEventConsumerDefinition : ShippingConsumerDefinition<OrderDeliveredEventConsumer>
{
    public OrderDeliveredEventConsumerDefinition() : base("order-delivered") { }
}

public sealed class ConfirmPickupCommandConsumerDefinition : ShippingConsumerDefinition<ConfirmPickupCommandConsumer>
{
    public ConfirmPickupCommandConsumerDefinition() : base("confirm-shipping") { }
}

public abstract class ShippingConsumerDefinition<TConsumer> : ConsumerDefinition<TConsumer>
    where TConsumer : class, IConsumer
{
    protected ShippingConsumerDefinition(string endpointName)
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
