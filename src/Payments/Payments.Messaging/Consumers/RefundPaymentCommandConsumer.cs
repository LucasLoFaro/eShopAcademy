using Domain.Common.Commands.Payments;
using Domain.Common.Events.Payments;
using MassTransit;

namespace Payments.Messaging.Consumers;

public sealed class RefundPaymentCommandConsumer : IConsumer<RefundPaymentCommand>
{
    public Task Consume(ConsumeContext<RefundPaymentCommand> context)
    {
        var command = context.Message;

        return context.Publish(new PaymentRefundedEvent
        {
            OrderId = command.OrderId,
            PaymentId = command.PaymentId,
            ProviderTransactionId = command.ProviderTransactionId,
            Amount = command.Amount,
            Reason = command.Reason
        }, context.CancellationToken);
    }
}
