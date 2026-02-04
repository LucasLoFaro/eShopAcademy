using Domain.Common.Commands.Shipping;
using MassTransit;

namespace Shipping.Service.Consumers;

public sealed class CancelShippingCommandConsumer : IConsumer<CancelShippingCommand>
{
    private readonly ILogger<CancelShippingCommandConsumer> _logger;

    public CancelShippingCommandConsumer(ILogger<CancelShippingCommandConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<CancelShippingCommand> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "[Shipping] Cancel requested for Order {OrderId}, Shipping {ShippingId}",
            message.OrderId,
            message.ShippingId);

        return Task.CompletedTask;
    }
}
