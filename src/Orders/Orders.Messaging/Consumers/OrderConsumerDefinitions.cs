using Infrastructure.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Orders.Messaging.Consumers;

public sealed class CancelOrderCommandConsumerDefinition
    : ConsumerDefinition<CancelOrderCommandConsumer>
{
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<CancelOrderCommandConsumer> consumerConfigurator,
        IRegistrationContext context) => Configure(endpointConfigurator, context);

    internal static void Configure(
        IReceiveEndpointConfigurator endpointConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(retry =>
        {
            retry.Handle<DbUpdateConcurrencyException>();
            retry.Handle<NpgsqlException>(exception => exception.IsTransient);
            retry.Intervals(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(1));
        });
        endpointConfigurator.UseEntityFrameworkOutbox<OrderDbContext>(context);
    }
}

public sealed class UpdateOrderStatusCommandConsumerDefinition
    : ConsumerDefinition<UpdateOrderStatusCommandConsumer>
{
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<UpdateOrderStatusCommandConsumer> consumerConfigurator,
        IRegistrationContext context) =>
        CancelOrderCommandConsumerDefinition.Configure(endpointConfigurator, context);
}

