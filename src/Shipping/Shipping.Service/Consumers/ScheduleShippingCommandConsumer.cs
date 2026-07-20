using Domain.Common.Commands.Shipping;
using Domain.Common.Events.Shipping;
using Shipping.Application.Clients;
using Shipping.Application.Data;
using Domain.Shipping.Entities;
using MassTransit;
using Shipping.Application;



namespace Shipping.Service.Consumers;

public class ScheduleShippingCommandConsumer : IConsumer<ScheduleShippingCommand>
{
    private readonly ILogger<ScheduleShippingCommandConsumer> _logger;
    private readonly IShippingInfoRepository _shippingInfoRepository;
    private readonly IShippingProviderClient _providerClient;
    private readonly IShippingOperationStore _operationStore;

    public ScheduleShippingCommandConsumer(
        ILogger<ScheduleShippingCommandConsumer> logger,
        IShippingInfoRepository shippingInfoRepository,
        IShippingProviderClient providerClient,
        IShippingOperationStore operationStore)
    {
        _logger = logger;
        _shippingInfoRepository = shippingInfoRepository;
        _providerClient = providerClient;
        _operationStore = operationStore;
    }

    public async Task Consume(ConsumeContext<ScheduleShippingCommand> context)
    {
        var message = context.Message;
        using var scope = _logger.BeginScope(new Dictionary<string, object> { ["OrderId"] = message.OrderId });

        if (!await _operationStore.TryBeginAsync(message.OrderId, context.CancellationToken))
        {
            _logger.LogDebug("Ignoring duplicate shipping schedule command.");
            return;
        }

        try
        {
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
                "Shipping scheduled for order {OrderId} with tracking {TrackingNumber}.",
                message.OrderId,
                result.TrackingNumber);
        }
        catch (Exception exception)
        {
            ShippingMetrics.RecordFailure("schedule", Classify(exception));
            ShippingMetrics.RecordConsumerRetry(nameof(ScheduleShippingCommandConsumer), Classify(exception));
            await _operationStore.AbandonAsync(message.OrderId, context.CancellationToken);
            throw;
        }

    }

    private static string Classify(Exception exception) => exception switch
    {
        HttpRequestException => "provider",
        MongoDB.Driver.MongoException => "database",
        TimeoutException or TaskCanceledException => "timeout",
        _ => "unexpected"
    };
}

