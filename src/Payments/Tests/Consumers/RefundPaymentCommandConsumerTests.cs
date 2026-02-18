using AutoFixture.Xunit2;
using Domain.Common.Commands.Payments;
using Domain.Common.Events.Payments;
using MassTransit;
using Moq;
using Payments.Messaging.Consumers;
using Xunit;

namespace Payments.Tests.Consumers;

public class RefundPaymentCommandConsumerTests
{
    [Theory]
    [AutoData]
    public async Task Consume_PublishesPaymentRefundedEventWithAllFields(RefundPaymentCommand command)
    {
        // Arrange
        var consumer = new RefundPaymentCommandConsumer();
        var context = new Mock<ConsumeContext<RefundPaymentCommand>>();
        context.Setup(c => c.Message).Returns(command);
        context.Setup(c => c.CancellationToken).Returns(CancellationToken.None);
        context.Setup(c => c.Publish(It.IsAny<PaymentRefundedEvent>(), It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);

        // Act
        await consumer.Consume(context.Object);

        // Assert: event published with every field from the command
        context.Verify(c => c.Publish(
            It.Is<PaymentRefundedEvent>(e =>
                e.OrderId == command.OrderId &&
                e.PaymentId == command.PaymentId &&
                e.ProviderTransactionId == command.ProviderTransactionId &&
                e.Amount == command.Amount &&
                e.Reason == command.Reason),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
