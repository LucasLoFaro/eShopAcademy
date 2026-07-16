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

    [Fact]
    public async Task Consume_PartialSagaUpdatesInAnyArrivalOrder_PreservesPaymentStockAndShippingFields()
    {
        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();
        var committedAt = DateTime.UtcNow;

        var commands = new[]
        {
            new UpdateOrderStatusCommand
            {
                OrderId = orderId,
                Status = "Paid",
                PaymentId = paymentId,
                PaymentStatus = "Captured",
                ProviderTransactionId = "provider-transaction",
                Amount = 99.99m,
                PaidAt = committedAt.AddSeconds(-1)
            },
            new UpdateOrderStatusCommand
            {
                OrderId = orderId,
                Status = "Processing",
                ReservationId = reservationId,
                StockCommittedAt = committedAt
            },
            new UpdateOrderStatusCommand
            {
                OrderId = orderId,
                Status = "Paid",
                ShippingStatus = "Scheduled",
                TrackingNumber = "SIM-REGRESSION",
                Carrier = "Simulator"
            }
        };

        var arrivalOrders = new[]
        {
            new[] { 0, 1, 2 },
            new[] { 0, 2, 1 },
            new[] { 1, 0, 2 },
            new[] { 1, 2, 0 },
            new[] { 2, 0, 1 },
            new[] { 2, 1, 0 }
        };

        foreach (var arrivalOrder in arrivalOrders)
        {
            var order = new Order { Id = orderId, Status = OrderStatus.Created };
            var orders = new Mock<IOrderRepository>();
            orders.Setup(repository => repository.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);
            orders.Setup(repository => repository.UpdateAsync(order, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var consumer = new UpdateOrderStatusCommandConsumer(
                orders.Object,
                new ConfigurationBuilder().Build(),
                NullLogger<UpdateOrderStatusCommandConsumer>.Instance);

            foreach (var commandIndex in arrivalOrder)
            {
                var context = new Mock<ConsumeContext<UpdateOrderStatusCommand>>();
                context.Setup(value => value.Message).Returns(commands[commandIndex]);
                context.Setup(value => value.CancellationToken).Returns(CancellationToken.None);
                await consumer.Consume(context.Object);
            }

            order.Payment.Id.Should().Be(paymentId);
            order.Payment.Status.Should().Be(PaymentStatus.Captured);
            order.Payment.ProviderTransactionId.Should().Be("provider-transaction");
            order.Stock.ReservationId.Should().Be(reservationId);
            order.Stock.CommittedAt.Should().Be(committedAt);
            order.Shipping.Status.Should().Be(ShippingStatus.Scheduled);
            order.Shipping.TrackingNumber.Should().Be("SIM-REGRESSION");
            order.Shipping.Carrier.Should().Be("Simulator");
        }
    }
}
