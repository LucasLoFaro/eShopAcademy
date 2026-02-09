using Domain.Common.Events.Orders;
using MassTransit;
using Shipping.Application.Data;

namespace Shipping.Service.Consumers;

public sealed class OrderDeliveredEventConsumer : IConsumer<OrderDeliveredEvent>
{
    private readonly IShippingInfoRepository _shippingInfoRepository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<OrderDeliveredEventConsumer> _logger;

    public OrderDeliveredEventConsumer(
        IShippingInfoRepository shippingInfoRepository,
        IPublishEndpoint publishEndpoint,
        ILogger<OrderDeliveredEventConsumer> logger)
    {
        _shippingInfoRepository = shippingInfoRepository;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderDeliveredEvent> context)
    {
        var incoming = context.Message;

        if (!string.IsNullOrWhiteSpace(incoming.CustomerEmail))
        {
            _logger.LogDebug("[Shipping] Delivered event for Order {OrderId} already contains customer email.", incoming.OrderId);
            return;
        }

        var shippingInfo = await _shippingInfoRepository.GetByOrderIdAsync(incoming.OrderId, context.CancellationToken);

        if (shippingInfo is null || string.IsNullOrWhiteSpace(shippingInfo.CustomerEmail))
        {
            _logger.LogWarning("[Shipping] No customer email available for Order {OrderId} when handling delivered event.", incoming.OrderId);
            return;
        }

        var enrichedEvent = new OrderDeliveredEvent
        {
            OrderId = incoming.OrderId,
            TrackingNumber = incoming.TrackingNumber,
            DeliveredAt = incoming.DeliveredAt,
            CustomerEmail = shippingInfo.CustomerEmail,
            CustomerName = shippingInfo.CustomerName ?? incoming.CustomerName
        };

        await _publishEndpoint.Publish(enrichedEvent, context.CancellationToken);

        _logger.LogInformation("[Shipping] Published enriched delivered event for Order {OrderId} to {Email}.",
            incoming.OrderId, shippingInfo.CustomerEmail);
    }
}
