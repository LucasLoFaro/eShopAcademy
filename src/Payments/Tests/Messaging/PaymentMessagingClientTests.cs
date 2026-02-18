using AutoFixture.Xunit2;
using Domain.Common.Events.Payments;
using FluentAssertions;
using Infrastructure.Messaging;
using MassTransit;
using Moq;
using Xunit;

namespace Payments.Tests.Messaging;

public class PaymentMessagingClientTests
{
    private readonly Mock<IPublishEndpoint> _publishEndpoint = new();
    private PaymentMessagingClient CreateSut() => new(_publishEndpoint.Object);

    [Theory]
    [AutoData]
    public async Task SendPaymentCreated_PublishesPaymentInitiatedEventWithCorrectFields(
        Guid orderId, string providerTransactionId)
    {
        // Arrange
        _publishEndpoint.Setup(p => p.Publish(It.IsAny<PaymentInitiatedEvent>(), It.IsAny<CancellationToken>()))
                        .Returns(Task.CompletedTask);

        // Act
        await CreateSut().SendPaymentCreated(orderId, providerTransactionId);

        // Assert
        _publishEndpoint.Verify(p => p.Publish(
            It.Is<PaymentInitiatedEvent>(e =>
                e.OrderId == orderId &&
                e.ProviderTransactionId == providerTransactionId),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task SendPaymentCompleted_PublishesPaymentCompletedEventWithCorrectFields(
        Guid orderId, string providerTransactionId)
    {
        // Arrange
        _publishEndpoint.Setup(p => p.Publish(It.IsAny<PaymentCompletedEvent>(), It.IsAny<CancellationToken>()))
                        .Returns(Task.CompletedTask);

        // Act
        await CreateSut().SendPaymentCompleted(orderId, providerTransactionId);

        // Assert
        _publishEndpoint.Verify(p => p.Publish(
            It.Is<PaymentCompletedEvent>(e =>
                e.OrderId == orderId &&
                e.ProviderTransactionId == providerTransactionId),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task SendPaymentFailed_PublishesPaymentFailedEventWithCorrectFields(
        Guid orderId, string providerTransactionId, string reason)
    {
        // Arrange
        _publishEndpoint.Setup(p => p.Publish(It.IsAny<PaymentFailedEvent>(), It.IsAny<CancellationToken>()))
                        .Returns(Task.CompletedTask);

        // Act
        await CreateSut().SendPaymentFailed(orderId, providerTransactionId, reason);

        // Assert
        _publishEndpoint.Verify(p => p.Publish(
            It.Is<PaymentFailedEvent>(e =>
                e.OrderId == orderId &&
                e.ProviderTransactionId == providerTransactionId &&
                e.Reason == reason),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
