using Domain.Common.Commands.Shipping;
using MassTransit;
using Shipping.Application.Clients;

namespace Shipping.Service.Consumers;

public sealed class ConfirmShippingCommandConsumer : IConsumer<ConfirmShippingCommand>
{
    private readonly IShippingProviderClient _providerClient;
    private readonly ILogger<ConfirmShippingCommandConsumer> _logger;

    public ConfirmShippingCommandConsumer(
        IShippingProviderClient providerClient,
        ILogger<ConfirmShippingCommandConsumer> logger)
    {
        _providerClient = providerClient;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ConfirmShippingCommand> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "[Shipping] Confirm requested for Order {OrderId}, Shipping {ShippingId}",
            message.OrderId,
            message.ShippingId);

        await _providerClient.ConfirmPickupAsync(message.ShippingId, message.OrderId, message.ReadyAt, context.CancellationToken);
    }
}
