using Domain.Common.Events.Orders;
using MassTransit;
using Microsoft.Extensions.Logging;
using Sellers.Application.Services;

namespace Sellers.EventsProcessor.Consumers;

public class OrderSellerSaleRegistrationRequestedConsumer : IConsumer<OrderSellerSaleRegistrationRequestedEvent>
{
    private readonly ILogger<OrderSellerSaleRegistrationRequestedConsumer> _logger;
    private readonly ISellerService _sellerService;

    public OrderSellerSaleRegistrationRequestedConsumer(
        ILogger<OrderSellerSaleRegistrationRequestedConsumer> logger,
        ISellerService sellerService)
    {
        _logger = logger;
        _sellerService = sellerService;
    }

    public async Task Consume(ConsumeContext<OrderSellerSaleRegistrationRequestedEvent> context)
    {
        var message = context.Message;

        var seller = await _sellerService.RegisterSaleAsync(
            message.SellerId,
            message.OrderId,
            message.OrderItemId,
            message.GrossAmount,
            message.CommissionAmount,
            message.Notes,
            context.CancellationToken);

        if (seller is null)
        {
            _logger.LogWarning(
                "[Sellers] Received sale registration event for unknown seller {SellerId}. OrderId {OrderId}.",
                message.SellerId,
                message.OrderId);
            return;
        }

        _logger.LogInformation(
            "[Sellers] Registered sale for seller {SellerId} on order {OrderId}.",
            message.SellerId,
            message.OrderId);
    }
}
