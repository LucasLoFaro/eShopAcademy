using MassTransit;

namespace Stock.Messaging.Processor.Consumers;

public sealed class CommitStockReservationConsumerDefinition : StockConsumerDefinition<CommitStockReservationConsumer>
{
    public CommitStockReservationConsumerDefinition() : base("commit-stock-reservation") { }
}

public sealed class ReleaseStockReservationConsumerDefinition : StockConsumerDefinition<ReleaseStockReservationConsumer>
{
    public ReleaseStockReservationConsumerDefinition() : base("release-stock-reservation") { }
}

public sealed class ProductPublishedConsumerDefinition : StockConsumerDefinition<ProductPublishedConsumer>
{
    public ProductPublishedConsumerDefinition() : base("product-published-stock") { }
}

public abstract class StockConsumerDefinition<TConsumer> : ConsumerDefinition<TConsumer>
    where TConsumer : class, IConsumer
{
    protected StockConsumerDefinition(string endpointName)
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
            retry.Handle<TimeoutException>();
            retry.Intervals(TimeSpan.FromMilliseconds(200), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3));
        });
        endpointConfigurator.UseMongoDbOutbox(context);
    }
}
