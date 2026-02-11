using Domain.Common.Commands.Shipping;
using MassTransit;
using Shipping.Application.Clients;

namespace Shipping.Service.Consumers;

public sealed class ConfirmPickupCommandConsumer : IConsumer<ConfirmPickupCommand>
{
    private readonly IShippingProviderClient _providerClient;
    private readonly ILogger<ConfirmPickupCommandConsumer> _logger;

    public ConfirmPickupCommandConsumer(
        IShippingProviderClient providerClient,
        ILogger<ConfirmPickupCommandConsumer> logger)
    {
        _providerClient = providerClient;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ConfirmPickupCommand> context)
    {
        var message = context.Message;

        await _providerClient.ConfirmPickupAsync(message.ShippingId, message.OrderId, message.ReadyAt, context.CancellationToken);

        _logger.LogInformation(
            "[Shipping] Confirm pickup requested for Order {OrderId}, Shipping {ShippingId}",
            message.OrderId,
            message.ShippingId);
    }
}
