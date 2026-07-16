using AutoFixture.Xunit2;
using Domain.Common.Commands.Orders;
using Domain.Common.Commands.Payments;
using Domain.Common.Commands.Stock;
using Domain.Common.Events.Orders;
using Domain.Orders.Entities;
using Domain.Orders.Enums;
using FluentAssertions;
using Infrastructure.Data;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Orders.Messaging.Consumers;
using Xunit;

namespace Orders.Tests.Messaging;

public class CancelOrderCommandConsumerTests
{
    private readonly Mock<IOrderRepository> _orders = new();

    private CancelOrderCommandConsumer CreateSut() =>
        new(_orders.Object, NullLogger<CancelOrderCommandConsumer>.Instance);

    [Theory]
    [AutoData]
    public async Task Consume_WhenOrderNotFound_PublishesNothing(CancelOrderCommand command)
    {
        // Arrange
        _orders.Setup(r => r.GetByIdAsync(command.OrderId, It.IsAny<CancellationToken>()))
               .ReturnsAsync((Order?)null);

        var context = new Mock<ConsumeContext<CancelOrderCommand>>();
        context.Setup(c => c.Message).Returns(command);
        context.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        // Act
        await CreateSut().Consume(context.Object);

        // Assert: no update, no publish
        _orders.Verify(r => r.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
        context.Verify(c => c.Publish(It.IsAny<OrderCancelledEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [AutoData]
    public async Task Consume_WhenOrderAlreadyCancelled_DoesNotUpdateOrPublish(CancelOrderCommand command)
    {
        // Arrange: order already in terminal state
        var order = new Order { Status = OrderStatus.Cancelled };
        order.GetType().GetProperty(nameof(Order.Id))!.SetValue(order, command.OrderId);

        _orders.Setup(r => r.GetByIdAsync(command.OrderId, It.IsAny<CancellationToken>()))
               .ReturnsAsync(order);

        var context = new Mock<ConsumeContext<CancelOrderCommand>>();
        context.Setup(c => c.Message).Returns(command);
        context.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        // Act
        await CreateSut().Consume(context.Object);

        // Assert
        _orders.Verify(r => r.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
        context.Verify(c => c.Publish(It.IsAny<OrderCancelledEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [AutoData]
    public async Task Consume_HappyPath_CancelsOrderAndPublishesEvents(CancelOrderCommand command)
    {
        // Arrange
        var order = new Order
        {
            Status = OrderStatus.Created,
            CustomerId = Guid.NewGuid(),
            Customer = new OrderCustomerInfo { Name = "Alice", Email = "alice@example.com" },
            Stock = new OrderStockReservationInfo { ReservationId = Guid.NewGuid() },
            Payment = new OrderPaymentInfo { Id = Guid.NewGuid() }
        };

        _orders.Setup(r => r.GetByIdAsync(command.OrderId, It.IsAny<CancellationToken>()))
               .ReturnsAsync(order);
        _orders.Setup(r => r.UpdateAsync(order, It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);

        var context = new Mock<ConsumeContext<CancelOrderCommand>>();
        var releaseStock = new Mock<ISendEndpoint>();
        var refundPayment = new Mock<ISendEndpoint>();
        context.Setup(c => c.Message).Returns(command);
        context.Setup(c => c.CancellationToken).Returns(CancellationToken.None);
        context.Setup(c => c.Publish(It.IsAny<OrderCancelledEvent>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        context.Setup(c => c.Publish(It.IsAny<OrderStatusUpdatedEvent>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        context.Setup(c => c.GetSendEndpoint(new Uri("queue:release-stock-reservation"))).ReturnsAsync(releaseStock.Object);
        context.Setup(c => c.GetSendEndpoint(new Uri("queue:refund-payment"))).ReturnsAsync(refundPayment.Object);
        releaseStock.Setup(e => e.Send(It.IsAny<ReleaseStockReservationCommand>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        refundPayment.Setup(e => e.Send(It.IsAny<RefundPaymentCommand>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        await CreateSut().Consume(context.Object);

        // Assert: order status set to Cancelled and persisted
        order.Status.Should().Be(OrderStatus.Cancelled);
        _orders.Verify(r => r.UpdateAsync(order, It.IsAny<CancellationToken>()), Times.Once);

        // Assert: facts are published and commands are sent to deterministic endpoints
        context.Verify(c => c.Publish(It.IsAny<OrderCancelledEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        releaseStock.Verify(e => e.Send(It.IsAny<ReleaseStockReservationCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        refundPayment.Verify(e => e.Send(It.IsAny<RefundPaymentCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
