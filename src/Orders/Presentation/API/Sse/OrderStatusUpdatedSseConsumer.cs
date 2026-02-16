using System.Text.Json;
using Domain.Common.Events.Orders;
using MassTransit;

namespace API.Sse;

public sealed class OrderStatusUpdatedSseConsumer : IConsumer<OrderStatusUpdatedEvent>
{
    private readonly OrderStatusStreamService _stream;

    public OrderStatusUpdatedSseConsumer(OrderStatusStreamService stream)
    {
        _stream = stream;
    }

    public Task Consume(ConsumeContext<OrderStatusUpdatedEvent> context)
    {
        var evt = context.Message;

        var payload = JsonSerializer.Serialize(new
        {
            orderId = evt.OrderId,
            status = evt.Status,
            occurredAt = evt.OccurredAt,
            trackingNumber = evt.TrackingNumber,
            carrier = evt.Carrier
        });

        _stream.Publish(evt.OrderId, payload);

        return Task.CompletedTask;
    }
}
