using MassTransit;

namespace Operations.Service.Consumers;

public sealed class PreparePackageCommandConsumerDefinition : OperationsConsumerDefinition<PreparePackageCommandConsumer>
{
    public PreparePackageCommandConsumerDefinition() : base("prepare-package") { }
}

public sealed class PackageSellerStampConsumerDefinition : OperationsConsumerDefinition<PackageSellerStampConsumer>
{
    public PackageSellerStampConsumerDefinition() : base("operations-seller-stamp") { }
}

public abstract class OperationsConsumerDefinition<TConsumer> : ConsumerDefinition<TConsumer>
    where TConsumer : class, IConsumer
{
    protected OperationsConsumerDefinition(string endpointName)
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
