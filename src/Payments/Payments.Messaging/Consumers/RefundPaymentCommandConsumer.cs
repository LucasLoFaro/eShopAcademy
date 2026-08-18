using Domain.Common.Commands.Payments;
using Domain.Common.Events.Payments;
using Infrastructure.Idempotency;
using Infrastructure.Messaging;
using Infrastructure.Observability;
using MassTransit;

namespace Payments.Messaging.Consumers;

public sealed class RefundPaymentCommandConsumer : IConsumer<RefundPaymentCommand>
{
    private readonly IPaymentOperationRegistry _operations;
    private readonly ILogger<RefundPaymentCommandConsumer> _logger;

    public RefundPaymentCommandConsumer(
        IPaymentOperationRegistry operations,
        ILogger<RefundPaymentCommandConsumer> logger)
    {
        _operations = operations;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<RefundPaymentCommand> context)
    {
        var command = context.Message;
        if (context.GetRetryAttempt() > 0)
            PaymentTelemetry.RecordRetry("refund");

        var stablePaymentKey = command.PaymentId != Guid.Empty
            ? command.PaymentId.ToString("N")
            : command.ProviderTransactionId;
        var operationKey = $"refund:{stablePaymentKey}";

        if (!_operations.TryBegin(operationKey))
        {
            _logger.LogInformation(
                "Ignoring duplicate refund command for order {OrderId} and payment {PaymentId}",
                command.OrderId,
                command.PaymentId);
            PaymentTelemetry.RecordResult("refund", "duplicate");
            return;
        }

        var messageId = PaymentMessageIdentity.Create(
            command.OrderId,
            stablePaymentKey,
            "refunded");

        try
        {
            await context.Publish(new PaymentRefundedEvent
            {
                EventId = messageId,
                CorrelationId = command.OrderId,
                OrderId = command.OrderId,
                PaymentId = command.PaymentId,
                ProviderTransactionId = command.ProviderTransactionId,
                Amount = command.Amount,
                Reason = command.Reason
            }, Pipe.Execute<PublishContext<PaymentRefundedEvent>>(publishContext =>
            {
                publishContext.MessageId = messageId;
                publishContext.CorrelationId = command.OrderId;
            }), context.CancellationToken);
            PaymentTelemetry.RecordResult("refund", "published");
        }
        catch
        {
            _operations.Abandon(operationKey);
            PaymentTelemetry.RecordFault("refund-payment", "publish");
            throw;
        }
    }
}

public sealed class RefundPaymentCommandConsumerDefinition
    : ConsumerDefinition<RefundPaymentCommandConsumer>
{
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<RefundPaymentCommandConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(retry =>
        {
            retry.Handle<HttpRequestException>();
            retry.Handle<TimeoutException>();
            retry.Intervals(TimeSpan.FromMilliseconds(200), TimeSpan.FromSeconds(1));
        });
        endpointConfigurator.UseInMemoryOutbox(context);
    }
}
