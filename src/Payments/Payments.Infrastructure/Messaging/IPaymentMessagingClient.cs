using Domain.Common.Events.Payments;
using MassTransit;

namespace Infrastructure.Messaging;

public class PaymentMessagingClient : IPaymentMessagingClient
{
    private readonly IPublishEndpoint _publishEndpoint;

    public PaymentMessagingClient(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public Task SendPaymentCreated(Guid orderId, string providerTransactionId, CancellationToken ct = default)
    {
        var messageId = PaymentMessageIdentity.Create(orderId, providerTransactionId, "initiated");
        return _publishEndpoint.Publish(new PaymentInitiatedEvent
        {
            EventId = messageId,
            CorrelationId = orderId,
            OrderId = orderId,
            ProviderTransactionId = providerTransactionId
        }, HeaderPipe<PaymentInitiatedEvent>(messageId, orderId), ct);
    }

    public Task SendPaymentCompleted(Guid orderId, string providerTransactionId, CancellationToken ct = default)
    {
        var messageId = PaymentMessageIdentity.Create(orderId, providerTransactionId, "completed");
        return _publishEndpoint.Publish(new PaymentCompletedEvent
        {
            EventId = messageId,
            CorrelationId = orderId,
            OrderId = orderId,
            ProviderTransactionId = providerTransactionId
        }, HeaderPipe<PaymentCompletedEvent>(messageId, orderId), ct);
    }

    public Task SendPaymentFailed(
        Guid orderId,
        string providerTransactionId,
        string reason,
        CancellationToken ct = default)
    {
        var messageId = PaymentMessageIdentity.Create(orderId, providerTransactionId, "failed");
        return _publishEndpoint.Publish(new PaymentFailedEvent
        {
            EventId = messageId,
            CorrelationId = orderId,
            OrderId = orderId,
            ProviderTransactionId = providerTransactionId,
            Reason = reason
        }, HeaderPipe<PaymentFailedEvent>(messageId, orderId), ct);
    }

    private static IPipe<PublishContext<T>> HeaderPipe<T>(Guid messageId, Guid orderId)
        where T : class
        => Pipe.Execute<PublishContext<T>>(context =>
        {
            context.MessageId = messageId;
            context.CorrelationId = orderId;
        });
}

public interface IPaymentMessagingClient
{
    Task SendPaymentCreated(Guid orderId, string providerTransactionId, CancellationToken ct = default);
    Task SendPaymentCompleted(Guid orderId, string providerTransactionId, CancellationToken ct = default);
    Task SendPaymentFailed(Guid orderId, string providerTransactionId, string reason, CancellationToken ct = default);
}
