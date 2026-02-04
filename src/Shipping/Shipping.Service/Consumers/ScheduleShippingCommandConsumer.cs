using Common.Domain.Commands.Shipping;
using MassTransit;


namespace Shipping.Service.Consumers;

public class ScheduleShippingCommandConsumer : IConsumer<ScheduleShippingCommand>
{
    private readonly ILogger<ScheduleShippingCommandConsumer> _logger;

    public ScheduleShippingCommandConsumer(ILogger<ScheduleShippingCommandConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<ScheduleShippingCommand> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "[Shipping] Schedule requested for Order {OrderId}, Destination {Destination}",
            message.OrderId,
            message.DestinationAddress);

        return Task.CompletedTask;
    }
}

