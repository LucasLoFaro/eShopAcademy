using Domain.Common.Events;
using MassTransit;

namespace Infrastructure.Messaging;

public class PaymentMessagingClient : IPaymentMessagingClient
{
    private readonly IPublishEndpoint _publishEndpoint;

    public PaymentMessagingClient(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public async Task SendPaymentCreated(Guid orderId, string providerTransactionId, CancellationToken ct = default)
    {
        var evt = new PaymentCreatedEvent
        {
            OrderId = orderId,
            ProviderTransactionId = providerTransactionId
        };

        await _publishEndpoint.Publish(evt, ct);
    }

    public async Task SendPaymentCompleted(Guid orderId, string providerTransactionId, CancellationToken ct = default)
    {
        var evt = new PaymentCompletedEvent
        {
            OrderId = orderId,
            ProviderTransactionId = providerTransactionId
        };

        await _publishEndpoint.Publish(evt, ct);
    }

    public async Task SendPaymentFailed(Guid orderId, string providerTransactionId, string reason, CancellationToken ct = default)
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
    Task SendPaymentCreated(Guid orderId, string providerTransactionId, CancellationToken ct = default);
    Task SendPaymentCompleted(Guid orderId, string providerTransactionId, CancellationToken ct = default);
    Task SendPaymentFailed(Guid orderId, string providerTransactionId, string reason, CancellationToken ct = default);
}