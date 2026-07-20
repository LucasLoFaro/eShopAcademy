using AutoFixture.Xunit2;
using Domain.Common.Commands.Payments;
using Domain.Common.Events.Payments;
using FluentAssertions;
using MassTransit;
using Moq;
using Payments.Messaging.Consumers;
using Xunit;
using Infrastructure.Idempotency;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;

namespace Payments.Tests.Consumers;

public class RefundPaymentCommandConsumerTests
{
    [Theory]
    [AutoData]
    public async Task Consume_PublishesPaymentRefundedEventWithAllFields(RefundPaymentCommand command)
    {
        // Arrange
        var consumer = new RefundPaymentCommandConsumer(
            new PaymentOperationRegistry(),
            NullLogger<RefundPaymentCommandConsumer>.Instance);
        var context = new Mock<ConsumeContext<RefundPaymentCommand>>();
        context.Setup(c => c.Message).Returns(command);
        context.Setup(c => c.CancellationToken).Returns(CancellationToken.None);
        context.Setup(c => c.Publish(
                It.IsAny<PaymentRefundedEvent>(),
                It.IsAny<IPipe<PublishContext<PaymentRefundedEvent>>>(),
                It.IsAny<CancellationToken>()))
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
            It.IsAny<IPipe<PublishContext<PaymentRefundedEvent>>>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task Consume_DuplicateRefund_PublishesOnlyOnce(RefundPaymentCommand command)
    {
        var consumer = new RefundPaymentCommandConsumer(
            new PaymentOperationRegistry(),
            NullLogger<RefundPaymentCommandConsumer>.Instance);
        var context = new Mock<ConsumeContext<RefundPaymentCommand>>();
        context.Setup(c => c.Message).Returns(command);
        context.Setup(c => c.CancellationToken).Returns(CancellationToken.None);
        context.Setup(c => c.Publish(It.IsAny<PaymentRefundedEvent>(),
                It.IsAny<IPipe<PublishContext<PaymentRefundedEvent>>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await consumer.Consume(context.Object);
        await consumer.Consume(context.Object);

        context.Verify(c => c.Publish(
            It.IsAny<PaymentRefundedEvent>(),
            It.IsAny<IPipe<PublishContext<PaymentRefundedEvent>>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task Consume_DuplicateRefund_DoesNotLogSensitiveReasonOrProviderCredential(
        RefundPaymentCommand command)
    {
        command = command with
        {
            Reason = "card=4111111111111111 secret=psp-credential",
            ProviderTransactionId = "psp-credential"
        };
        var logger = new CollectingLogger<RefundPaymentCommandConsumer>();
        var consumer = new RefundPaymentCommandConsumer(new PaymentOperationRegistry(), logger);
        var context = new Mock<ConsumeContext<RefundPaymentCommand>>();
        context.Setup(c => c.Message).Returns(command);
        context.Setup(c => c.CancellationToken).Returns(CancellationToken.None);
        context.Setup(c => c.Publish(
                It.IsAny<PaymentRefundedEvent>(),
                It.IsAny<IPipe<PublishContext<PaymentRefundedEvent>>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await consumer.Consume(context.Object);
        await consumer.Consume(context.Object);

        var output = string.Join(Environment.NewLine, logger.Messages);
        output.Should().NotContain("4111111111111111");
        output.Should().NotContain("psp-credential");
    }

    private sealed class CollectingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Messages.Add(formatter(state, exception));
    }
}
