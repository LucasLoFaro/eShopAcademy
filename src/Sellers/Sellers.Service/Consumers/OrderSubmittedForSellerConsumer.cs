using Domain.Common.Events.Orders;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Sellers.Service.Consumers;

public class OrderSubmittedForSellerConsumer : IConsumer<OrderSubmittedEvent>
{
    private readonly ILogger<OrderSubmittedForSellerConsumer> _logger;

    public OrderSubmittedForSellerConsumer(ILogger<OrderSubmittedForSellerConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<OrderSubmittedEvent> context)
    {
        _logger.LogInformation(
            "[Sellers] Received order submitted event for order {OrderId}. Seller routing pending order-item seller attribution support.",
            context.Message.OrderId);

        return Task.CompletedTask;
    }
}
