using Domain.Common.Commands.Shipping;
using Domain.Common.Events.Shipping;
using Shipping.Application.Clients;
using Shipping.Application.Data;
using Domain.Shipping.Entities;
using MassTransit;



namespace Shipping.Service.Consumers;

public class ScheduleShippingCommandConsumer : IConsumer<ScheduleShippingCommand>
{
    private readonly ILogger<ScheduleShippingCommandConsumer> _logger;
    private readonly IShippingInfoRepository _shippingInfoRepository;
    private readonly IShippingProviderClient _providerClient;

    public ScheduleShippingCommandConsumer(
        ILogger<ScheduleShippingCommandConsumer> logger,
        IShippingInfoRepository shippingInfoRepository,
        IShippingProviderClient providerClient)
    {
        _logger = logger;
        _shippingInfoRepository = shippingInfoRepository;
        _providerClient = providerClient;
    }

    public async Task Consume(ConsumeContext<ScheduleShippingCommand> context)
    {
        var message = context.Message;

        if (!string.IsNullOrWhiteSpace(message.CustomerEmail))
        {
            await _shippingInfoRepository.UpsertAsync(new ShippingInfo
            {
                OrderId = message.OrderId,
                CustomerEmail = message.CustomerEmail
            }, context.CancellationToken);
        }

        var shipping = new Domain.Shipping.Entities.Shipping
        {
            OrderId = message.OrderId,
            Address = new Domain.Shipping.Entities.Address
            {
                Street = message.DestinationAddress ?? string.Empty
            }
        };

        var result = await _providerClient.ScheduleShippingAsync(shipping, context.CancellationToken);

        await context.Publish(new ShippingScheduledEvent
        {
            OrderId = message.OrderId,
            ShipmentId = result.ShipmentId,
            Carrier = result.Carrier,
            TrackingNumber = result.TrackingNumber,
            DestinationAddress = message.DestinationAddress
        }, context.CancellationToken);

        _logger.LogInformation(
            "[Shipping] Schedule requested for Order {OrderId}, Destination {Destination}, Tracking {TrackingNumber}",
            message.OrderId,
            message.DestinationAddress,
            result.TrackingNumber);

    }
}

