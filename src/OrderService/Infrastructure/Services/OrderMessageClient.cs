using Azure.Identity;
using Core.Domain.Commands;
using Core.Domain.Contracts;
using Infrastructure.Services.Interfaces;
using Infrastructure.Services.Settings;
using MassTransit;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

/// <summary>
/// Concrete implementation of <see cref="IOrderMessagingService"/> that
/// uses MassTransit to publish order commands to Azure Service Bus.
/// </summary>
public class OrderMessageClient : IOrderMessagingService
{
    private readonly IBusControl _bus;

    public OrderMessageClient(IOptionsMonitor<ServiceBusSettings> settings)
    {
        var credential = new DefaultAzureCredential();

        // Create a dedicated bus instance for publishing messages.  The
        // connection string and credential are loaded from the injected
        // ServiceBusSettings.  Bus.Start is invoked synchronously here
        // which is acceptable for a singleton service; MassTransit will
        // handle reconnects internally.
        _bus = Bus.Factory.CreateUsingAzureServiceBus(cfg =>
        {
            cfg.Host($"sb://{settings.CurrentValue.Host}/", h =>
            {
                h.TokenCredential = credential;
            });
        });

        _bus.Start();
    }

    /// <inheritdoc />
    public async Task SubmitOrder(OrderRequest orderRequest)
    {
        // Generate a new correlation identifier for the order.  This
        // identifier will be used by the saga state machine to track
        // progress across multiple services.
        var command = new SubmitOrder
        {
            OrderId = Guid.NewGuid(),
            CustomerId = orderRequest.CustomerId,
            Items = orderRequest.Items
        };

        await _bus.Publish(command);
    }
}