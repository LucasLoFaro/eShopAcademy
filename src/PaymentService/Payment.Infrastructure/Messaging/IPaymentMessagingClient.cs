using Domain.Events;
using MassTransit;

namespace Infrastructure.Messaging;

public class PaymentMessagingClient : IPaymentMessagingClient
{
    private readonly IPublishEndpoint _publishEndpoint;

    public PaymentMessagingClient(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public async Task SendPaymentCreated(string orderId, string providerTransactionId, CancellationToken ct = default)
    {
        var evt = new PaymentCreatedEvent
        {
            OrderId = orderId,
            ProviderTransactionId = providerTransactionId
        };

        await _publishEndpoint.Publish(evt, ct);
    }

    public async Task SendPaymentCompleted(string orderId, string providerTransactionId, CancellationToken ct = default)
    {
        var evt = new PaymentCompletedEvent
        {
            OrderId = orderId,
            ProviderTransactionId = providerTransactionId
        };

        await _publishEndpoint.Publish(evt, ct);
    }

    public async Task SendPaymentFailed(string orderId, string providerTransactionId, string reason, CancellationToken ct = default)
    {
        var evt = new PaymentFailedEvent
        {
            OrderId = orderId,
            PaymentSessionId = providerTransactionId,
            Reason = reason
        };

        await _publishEndpoint.Publish(evt, ct);
    }
}

public interface IPaymentMessagingClient
{
    Task SendPaymentCreated(string orderId, string providerTransactionId, CancellationToken ct = default);
    Task SendPaymentCompleted(string orderId, string providerTransactionId, CancellationToken ct = default);
    Task SendPaymentFailed(string orderId, string providerTransactionId, string reason, CancellationToken ct = default);
}