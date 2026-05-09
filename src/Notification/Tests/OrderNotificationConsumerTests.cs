using AutoFixture.Xunit2;
using Domain.Common.Events.Orders;
using Domain.Notification.Entities;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;
using Moq;
using NotificationService;
using NotificationService.Data;
using NotificationService.Templates;
using Xunit;

namespace Notification.Tests;

public class OrderNotificationConsumerTests
{
    private readonly Mock<IEmailSender> _emailSender = new();
    private readonly Mock<IEmailTemplateRenderer> _renderer = new();

    // Mock the DbContext so InsertOneAsync completes instantly instead of timing out against a real MongoDB
    private readonly Mock<NotificationDbContext> _dbContextMock;

    public OrderNotificationConsumerTests()
    {
        _dbContextMock = new Mock<NotificationDbContext>("mongodb://localhost:27017", "notifications-test");
        var mockCollection = new Mock<IMongoCollection<NotificationMessage>>();
        _dbContextMock.Setup(db => db.Notifications).Returns(mockCollection.Object);
    }

    private OrderNotificationConsumer CreateSut() =>
        new(_emailSender.Object, _renderer.Object, _dbContextMock.Object, NullLogger<OrderNotificationConsumer>.Instance);

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

    [Theory]
    [InlineData("Delivered", "OrderDelivered")]
    [InlineData("Cancelled", "OrderCancelled")]
    public async Task ConsumeOrderStatusUpdated_NewStatuses_RendersCorrectTemplateAndSendsEmail(
        string status, string expectedTemplate)
    {
        // Arrange
        var evt = new OrderStatusUpdatedEvent
        {
            OrderId = Guid.NewGuid(),
            CustomerEmail = "user@example.com",
            CustomerName = "Alice",
            Status = status,
            Reason = "Test reason"
        };

        _renderer.Setup(r => r.Render(expectedTemplate, It.IsAny<Dictionary<string, string>>()))
                 .Returns("<html/>");
        _emailSender.Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                    .Returns(Task.CompletedTask);

        var context = new Mock<ConsumeContext<OrderStatusUpdatedEvent>>();
        context.Setup(c => c.Message).Returns(evt);

        // Act
        await CreateSut().Consume(context.Object);

        // Assert: correct template selected and email sent
        _renderer.Verify(r => r.Render(expectedTemplate, It.IsAny<Dictionary<string, string>>()), Times.Once);
        _emailSender.Verify(s => s.SendAsync("user@example.com", It.IsAny<string>(), "<html/>"), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task ConsumeOrderSubmitted_WithEmail_IncludesPlaceholdersInTemplate(OrderSubmittedEvent evt)
    {
        // Arrange
        var emailEvent = evt with { CustomerEmail = "test@example.com", CustomerName = "Bob" };

        _renderer.Setup(r => r.Render("OrderSubmitted", It.IsAny<Dictionary<string, string>>()))
                 .Returns("<html/>");
        _emailSender.Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                    .Returns(Task.CompletedTask);

        var context = new Mock<ConsumeContext<OrderSubmittedEvent>>();
        context.Setup(c => c.Message).Returns(emailEvent);

        // Act
        await CreateSut().Consume(context.Object);

        // Assert: placeholders include customer name and order number
        _renderer.Verify(r => r.Render("OrderSubmitted",
            It.Is<Dictionary<string, string>>(d =>
                d["CustomerName"] == "Bob" &&
                d["OrderNumber"] == emailEvent.OrderId.ToString())),
            Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task ConsumeOrderStatusUpdated_Cancelled_IncludesReasonInPlaceholders(OrderStatusUpdatedEvent evt)
    {
        // Arrange
        var cancelledEvent = evt with
        {
            CustomerEmail = "user@example.com",
            Status = "Cancelled",
            Reason = "Out of stock"
        };

        _renderer.Setup(r => r.Render("OrderCancelled", It.IsAny<Dictionary<string, string>>()))
                 .Returns("<html/>");
        _emailSender.Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                    .Returns(Task.CompletedTask);

        var context = new Mock<ConsumeContext<OrderStatusUpdatedEvent>>();
        context.Setup(c => c.Message).Returns(cancelledEvent);

        // Act
        await CreateSut().Consume(context.Object);

        // Assert: reason placeholder is populated from event
        _renderer.Verify(r => r.Render("OrderCancelled",
            It.Is<Dictionary<string, string>>(d => d["Reason"] == "Out of stock")),
            Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task ConsumeOrderStatusUpdated_Shipped_IncludesTrackingInfoInPlaceholders(OrderStatusUpdatedEvent evt)
    {
        // Arrange
        var shippedEvent = evt with
        {
            CustomerEmail = "user@example.com",
            Status = "Shipped",
            TrackingNumber = "TRK-12345",
            Carrier = "FedEx"
        };

        _renderer.Setup(r => r.Render("OrderShipped", It.IsAny<Dictionary<string, string>>()))
                 .Returns("<html/>");
        _emailSender.Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                    .Returns(Task.CompletedTask);

        var context = new Mock<ConsumeContext<OrderStatusUpdatedEvent>>();
        context.Setup(c => c.Message).Returns(shippedEvent);

        // Act
        await CreateSut().Consume(context.Object);

        // Assert: tracking info placeholders are populated
        _renderer.Verify(r => r.Render("OrderShipped",
            It.Is<Dictionary<string, string>>(d =>
                d["TrackingNumber"] == "TRK-12345" &&
                d["Carrier"] == "FedEx")),
            Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task ConsumeOrderStatusUpdated_Paid_IncludesAmountInPlaceholders(OrderStatusUpdatedEvent evt)
    {
        // Arrange
        var paidEvent = evt with
        {
            CustomerEmail = "user@example.com",
            Status = "Paid",
            Amount = 99.99m,
            Currency = "EUR"
        };

        _renderer.Setup(r => r.Render("PaymentConfirmed", It.IsAny<Dictionary<string, string>>()))
                 .Returns("<html/>");
        _emailSender.Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                    .Returns(Task.CompletedTask);

        var context = new Mock<ConsumeContext<OrderStatusUpdatedEvent>>();
        context.Setup(c => c.Message).Returns(paidEvent);

        // Act
        await CreateSut().Consume(context.Object);

        // Assert: amount and currency placeholders are populated
        _renderer.Verify(r => r.Render("PaymentConfirmed",
            It.Is<Dictionary<string, string>>(d =>
                d["Amount"] == 99.99m.ToString("N2") &&
                d["Currency"] == "EUR")),
            Times.Once);
    }
}
