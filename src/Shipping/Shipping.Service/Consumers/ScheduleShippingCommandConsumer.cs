using Domain.Common.Commands.Shipping;
using Shipping.Application.Data;
using Domain.Shipping.Entities;
using MassTransit;



namespace Shipping.Service.Consumers;

public class ScheduleShippingCommandConsumer : IConsumer<ScheduleShippingCommand>
{
    private readonly ILogger<ScheduleShippingCommandConsumer> _logger;
    private readonly IShippingInfoRepository _shippingInfoRepository;

    public ScheduleShippingCommandConsumer(
        ILogger<ScheduleShippingCommandConsumer> logger,
        IShippingInfoRepository shippingInfoRepository)
    {
        _logger = logger;
        _shippingInfoRepository = shippingInfoRepository;
    }

    public async Task Consume(ConsumeContext<ScheduleShippingCommand> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "[Shipping] Schedule requested for Order {OrderId}, Destination {Destination}",
            message.OrderId,
            message.DestinationAddress);

        if (!string.IsNullOrWhiteSpace(message.CustomerEmail))
        {
            await _shippingInfoRepository.UpsertAsync(new ShippingInfo
            {
                OrderId = message.OrderId,
                CustomerEmail = message.CustomerEmail
            }, context.CancellationToken);
        }
    }
}

