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

    public async Task SendPaymentCompleted(string orderId, string paymentSessionId, CancellationToken ct = default)
    {
        var evt = new PaymentCompletedEvent
        {
            OrderId = orderId,
            PaymentSessionId = paymentSessionId
        };

        await _publishEndpoint.Publish(evt, ct);
    }

    public async Task SendPaymentFailed(string orderId, string paymentSessionId, string reason, CancellationToken ct = default)
    {
        var evt = new PaymentFailedEvent
        {
            OrderId = orderId,
            PaymentSessionId = paymentSessionId,
            Reason = reason
        };

        await _publishEndpoint.Publish(evt, ct);
    }
}

public interface IPaymentMessagingClient
{
    Task SendPaymentCompleted(string orderId, string paymentSessionId, CancellationToken ct = default);
    Task SendPaymentFailed(string orderId, string paymentSessionId, string reason, CancellationToken ct = default);
}