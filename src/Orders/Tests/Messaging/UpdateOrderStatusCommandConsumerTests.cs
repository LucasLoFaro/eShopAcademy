using AutoFixture.Xunit2;
using Domain.Common.Commands.Orders;
using Domain.Orders.Entities;
using Domain.Orders.Enums;
using FluentAssertions;
using Infrastructure.Data;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Orders.Messaging.Consumers;
using Xunit;

namespace Orders.Tests.Messaging;

public class UpdateOrderStatusCommandConsumerTests
{
    private readonly Mock<IOrderRepository> _orders = new();

    private UpdateOrderStatusCommandConsumer CreateSut() =>
        new(
            _orders.Object,
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Sellers:CommissionRate"] = "0.10"
                })
                .Build(),
            NullLogger<UpdateOrderStatusCommandConsumer>.Instance);

    [Theory]
    [AutoData]
    public async Task Consume_WhenStatusIsInvalid_DoesNotUpdateOrder(UpdateOrderStatusCommand command)
    {
        // Arrange: unrecognised status string
        var invalidCmd = command with { Status = "NotAValidStatus" };
        var context = new Mock<ConsumeContext<UpdateOrderStatusCommand>>();
        context.Setup(c => c.Message).Returns(invalidCmd);
        context.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        // Act
        await CreateSut().Consume(context.Object);

        // Assert: no DB lookup, no update
        _orders.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _orders.Verify(r => r.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [AutoData]
    public async Task Consume_WhenOrderNotFound_DoesNotUpdate(UpdateOrderStatusCommand command)
    {
        // Arrange
        var cmd = command with { Status = "Created" };
        _orders.Setup(r => r.GetByIdAsync(cmd.OrderId, It.IsAny<CancellationToken>()))
               .ReturnsAsync((Order?)null);

        var context = new Mock<ConsumeContext<UpdateOrderStatusCommand>>();
        context.Setup(c => c.Message).Returns(cmd);
        context.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        // Act
        await CreateSut().Consume(context.Object);

        // Assert
        _orders.Verify(r => r.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [AutoData]
    public async Task Consume_WhenOrderAlreadyDelivered_DoesNotUpdate(UpdateOrderStatusCommand command)
    {
        // Arrange: terminal state should not be overwritten
        var cmd = command with { Status = "Processing" };
        var order = new Order { Status = OrderStatus.Delivered };

        _orders.Setup(r => r.GetByIdAsync(cmd.OrderId, It.IsAny<CancellationToken>()))
               .ReturnsAsync(order);

        var context = new Mock<ConsumeContext<UpdateOrderStatusCommand>>();
        context.Setup(c => c.Message).Returns(cmd);
        context.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        // Act
        await CreateSut().Consume(context.Object);

        // Assert: no update attempted on a delivered order
        _orders.Verify(r => r.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [AutoData]
    public async Task Consume_HappyPath_UpdatesOrderStatusAndPersists(UpdateOrderStatusCommand command)
    {
        // Arrange
        var cmd = command with { Status = "Processing", PaymentStatus = null, ShippingStatus = null };
        var order = new Order { Status = OrderStatus.Created };

        _orders.Setup(r => r.GetByIdAsync(cmd.OrderId, It.IsAny<CancellationToken>()))
               .ReturnsAsync(order);
        _orders.Setup(r => r.UpdateAsync(order, It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);

        var context = new Mock<ConsumeContext<UpdateOrderStatusCommand>>();
        context.Setup(c => c.Message).Returns(cmd);
        context.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        // Act
        await CreateSut().Consume(context.Object);

        // Assert: status updated and saved
        order.Status.Should().Be(OrderStatus.Processing);
        _orders.Verify(r => r.UpdateAsync(order, It.IsAny<CancellationToken>()), Times.Once);
    }
}
