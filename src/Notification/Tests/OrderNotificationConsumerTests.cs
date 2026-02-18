using AutoFixture.Xunit2;
using Domain.Common.Events.Orders;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NotificationService;
using NotificationService.Templates;
using Xunit;

namespace Notification.Tests;

public class OrderNotificationConsumerTests
{
    private readonly Mock<IEmailSender> _emailSender = new();
    private readonly Mock<IEmailTemplateRenderer> _renderer = new();

    private OrderNotificationConsumer CreateSut() =>
        new(_emailSender.Object, _renderer.Object, NullLogger<OrderNotificationConsumer>.Instance);

    [Theory]
    [AutoData]
    public async Task ConsumeOrderSubmitted_WithEmail_RendersTemplateAndSendsEmail(OrderSubmittedEvent evt)
    {
        // Arrange
        var emailEvent = evt with { CustomerEmail = "test@example.com" };
        const string renderedHtml = "<h1>Order received</h1>";

        _renderer.Setup(r => r.Render("OrderSubmitted", It.IsAny<Dictionary<string, string>>()))
                 .Returns(renderedHtml);
        _emailSender.Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                    .Returns(Task.CompletedTask);

        var context = new Mock<ConsumeContext<OrderSubmittedEvent>>();
        context.Setup(c => c.Message).Returns(emailEvent);

        // Act
        await CreateSut().Consume(context.Object);

        // Assert: template rendered and email sent to the customer
        _renderer.Verify(r => r.Render("OrderSubmitted", It.IsAny<Dictionary<string, string>>()), Times.Once);
        _emailSender.Verify(s => s.SendAsync("test@example.com", It.IsAny<string>(), renderedHtml), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task ConsumeOrderSubmitted_WithoutEmail_SkipsSendingEmail(OrderSubmittedEvent evt)
    {
        // Arrange: missing email on event
        var noEmailEvent = evt with { CustomerEmail = string.Empty };

        var context = new Mock<ConsumeContext<OrderSubmittedEvent>>();
        context.Setup(c => c.Message).Returns(noEmailEvent);

        // Act
        await CreateSut().Consume(context.Object);

        // Assert: nothing rendered or sent
        _renderer.Verify(r => r.Render(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()), Times.Never);
        _emailSender.Verify(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("Paid", "PaymentConfirmed")]
    [InlineData("ReadyForPickup", "ReadyForPickup")]
    [InlineData("Shipped", "OrderShipped")]
    [InlineData("Delivered", "OrderDelivered")]
    [InlineData("Cancelled", "OrderCancelled")]
    public async Task ConsumeOrderStatusUpdated_KnownStatus_RendersCorrectTemplateAndSendsEmail(
        string status, string expectedTemplate)
    {
        // Arrange
        var evt = new OrderStatusUpdatedEvent
        {
            OrderId = Guid.NewGuid(),
            CustomerEmail = "customer@example.com",
            CustomerName = "Bob",
            Status = status
        };

        _renderer.Setup(r => r.Render(expectedTemplate, It.IsAny<Dictionary<string, string>>()))
                 .Returns("<html/>");
        _emailSender.Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                    .Returns(Task.CompletedTask);

        var context = new Mock<ConsumeContext<OrderStatusUpdatedEvent>>();
        context.Setup(c => c.Message).Returns(evt);

        // Act
        await CreateSut().Consume(context.Object);

        // Assert: correct template selected for each status
        _renderer.Verify(r => r.Render(expectedTemplate, It.IsAny<Dictionary<string, string>>()), Times.Once);
        _emailSender.Verify(s => s.SendAsync("customer@example.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task ConsumeOrderStatusUpdated_UnknownStatus_SkipsSendingEmail(OrderStatusUpdatedEvent evt)
    {
        // Arrange: status that has no template mapping
        var unknownStatusEvent = evt with { CustomerEmail = "customer@example.com", Status = "UnknownStatus" };

        var context = new Mock<ConsumeContext<OrderStatusUpdatedEvent>>();
        context.Setup(c => c.Message).Returns(unknownStatusEvent);

        // Act
        await CreateSut().Consume(context.Object);

        // Assert: no email sent for unmapped status
        _emailSender.Verify(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
