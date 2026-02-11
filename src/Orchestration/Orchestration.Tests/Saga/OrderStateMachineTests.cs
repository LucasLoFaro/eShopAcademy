using System.Linq;
using AutoFixture.Xunit2;
using Application.Saga;
using Domain.Common.Commands.Basket;
using Domain.Common.Commands.Operations;
using Domain.Common.Commands.Orders;
using Domain.Common.Commands.Payments;
using Domain.Common.Commands.Shipping;
using Domain.Common.Commands.Stock;
using Domain.Common.Events.Orders;
using Domain.Common.Events.Payments;
using Domain.Common.Events.Shipping;
using Domain.Common.Events.Stock;
using Domain.Common.States;
using MassTransit;
using MassTransit.Testing;
using Xunit;

namespace Orders.Tests.Orchestration.Saga;

public class OrderStateMachineTests : IAsyncLifetime
{
    private InMemoryTestHarness _harness = null!;
    private ISagaStateMachineTestHarness<OrderStateMachine, OrderState> _sagaHarness = null!;
    private OrderStateMachine _machine = null!;

    public async Task InitializeAsync()
    {
        _harness = new InMemoryTestHarness();

        _machine = new OrderStateMachine();

        _sagaHarness = _harness.StateMachineSaga<OrderState, OrderStateMachine>(_machine);

        await _harness.Start();
    }

    public async Task DisposeAsync()
    {
        if (_harness != null)
        {
            await _harness.Stop();
        }
    }

    [Theory]
    [AutoData]
    public async Task OnPaymentCompleted_ShouldPublishStockCommitEmptyBasketAndShippingCommands(
        OrderSubmittedEvent submittedEvent,
        PaymentCompletedEvent paymentCompletedEvent)
    {
        await _harness.InputQueueSendEndpoint.Send(submittedEvent);

        Assert.True(await _harness.Consumed.Any<OrderSubmittedEvent>());

        var submittedId = await _sagaHarness.Exists(submittedEvent.OrderId, _machine.Submitted);
        Assert.NotNull(submittedId);

        paymentCompletedEvent.OrderId = submittedEvent.OrderId;
        await _harness.InputQueueSendEndpoint.Send(paymentCompletedEvent);

        Assert.True(await _harness.Consumed.Any<PaymentCompletedEvent>());

        Assert.True(await _harness.Published.Any<CommitStockReservationCommand>());
        Assert.True(await _harness.Published.Any<EmptyBasketCommand>());
        Assert.True(await _harness.Published.Any<ScheduleShippingCommand>());

        var commitConsume = await _harness.Published
            .SelectAsync<CommitStockReservationCommand>()
            .FirstOrDefault();

        Assert.NotNull(commitConsume);
        Assert.Equal(submittedEvent.OrderId, commitConsume.Context.Message.OrderId);
        Assert.Equal(submittedEvent.ReservationId, commitConsume.Context.Message.ReservationId);

        var emptyBasketConsume = await _harness.Published
            .SelectAsync<EmptyBasketCommand>()
            .FirstOrDefault();

        Assert.NotNull(emptyBasketConsume);
        Assert.Equal(submittedEvent.OrderId, emptyBasketConsume.Context.Message.OrderId);
        Assert.Equal(submittedEvent.CustomerId, emptyBasketConsume.Context.Message.ClientId);

        var scheduleConsume = await _harness.Published
            .SelectAsync<ScheduleShippingCommand>()
            .FirstOrDefault();

        Assert.NotNull(scheduleConsume);
        Assert.Equal(submittedEvent.OrderId, scheduleConsume.Context.Message.OrderId);
        Assert.Equal(submittedEvent.CustomerEmail, scheduleConsume.Context.Message.CustomerEmail);
    }

    [Theory]
    [AutoData]
    public async Task OnPaymentFailed_ShouldPublishCancelOrderCommand(
        OrderSubmittedEvent submittedEvent,
        PaymentFailedEvent paymentFailedEvent)
    {
        await _harness.InputQueueSendEndpoint.Send(submittedEvent);

        Assert.True(await _harness.Consumed.Any<OrderSubmittedEvent>());

        // Ensure saga created
        var submittedId = await _sagaHarness.Exists(submittedEvent.OrderId, _machine.Submitted);
        Assert.NotNull(submittedId);

        // Act: payment failed
        paymentFailedEvent.OrderId = submittedEvent.OrderId;
        await _harness.InputQueueSendEndpoint.Send(paymentFailedEvent);

        Assert.True(await _harness.Consumed.Any<PaymentFailedEvent>());

        // Assert CancelOrderCommand was published with reason + customer data
        Assert.True(await _harness.Published.Any<CancelOrderCommand>());

        var cancelConsume = await _harness.Published
            .SelectAsync<CancelOrderCommand>()
            .FirstOrDefault();

        Assert.NotNull(cancelConsume);

        var cancel = cancelConsume.Context.Message;

        Assert.Equal(submittedEvent.OrderId, cancel.OrderId);
        Assert.Equal(paymentFailedEvent.Reason, cancel.Reason);
        Assert.False(string.IsNullOrWhiteSpace(cancel.CustomerEmail));
        Assert.False(string.IsNullOrWhiteSpace(cancel.CustomerName));
    }


    [Theory]
    [AutoData]
    public async Task OnStockReservationCommitted_ShouldPublishPreparePackageCommand(
        OrderSubmittedEvent submittedEvent,
        StockReservationCommittedEvent committedEvent)
    {
        await _harness.InputQueueSendEndpoint.Send(submittedEvent);

        Assert.True(await _harness.Consumed.Any<OrderSubmittedEvent>());

        var submittedId = await _sagaHarness.Exists(submittedEvent.OrderId, _machine.Submitted);
        Assert.NotNull(submittedId);

        committedEvent = committedEvent with
        {
            OrderId = submittedEvent.OrderId,
            ReservationId = submittedEvent.ReservationId
        };
        await _harness.InputQueueSendEndpoint.Send(committedEvent);

        Assert.True(await _harness.Consumed.Any<StockReservationCommittedEvent>());
        Assert.True(await _harness.Published.Any<PreparePackageCommand>());

        var prepareConsume = await _harness.Published
            .SelectAsync<PreparePackageCommand>()
            .FirstOrDefault();

        Assert.NotNull(prepareConsume);
        Assert.Equal(submittedEvent.OrderId, prepareConsume.Context.Message.OrderId);
        Assert.Equal(submittedEvent.ReservationId, prepareConsume.Context.Message.ReservationId);
    }

    [Theory]
    [AutoData]
    public async Task OnStockReservationCommitFailed_ShouldPublishRefundAndCancelCommands(
        OrderSubmittedEvent submittedEvent,
        PaymentCompletedEvent paymentCompletedEvent,
        StockReservationCommitFailedEvent commitFailedEvent)
    {
        await _harness.InputQueueSendEndpoint.Send(submittedEvent);

        Assert.True(await _harness.Consumed.Any<OrderSubmittedEvent>());

        var submittedId = await _sagaHarness.Exists(submittedEvent.OrderId, _machine.Submitted);
        Assert.NotNull(submittedId);

        paymentCompletedEvent.OrderId = submittedEvent.OrderId;
        await _harness.InputQueueSendEndpoint.Send(paymentCompletedEvent);

        Assert.True(await _harness.Consumed.Any<PaymentCompletedEvent>());

        commitFailedEvent.OrderId = submittedEvent.OrderId;
        await _harness.InputQueueSendEndpoint.Send(commitFailedEvent);

        Assert.True(await _harness.Consumed.Any<StockReservationCommitFailedEvent>());
        Assert.True(await _harness.Published.Any<RefundPaymentCommand>());
        Assert.True(await _harness.Published.Any<CancelShippingCommand>());
        Assert.True(await _harness.Published.Any<CancelOrderCommand>());

        var refundConsume = await _harness.Published
            .SelectAsync<RefundPaymentCommand>()
            .FirstOrDefault();

        Assert.NotNull(refundConsume);
        Assert.Equal(submittedEvent.OrderId, refundConsume.Context.Message.OrderId);
        Assert.Equal(paymentCompletedEvent.PaymentId, refundConsume.Context.Message.PaymentId);
        Assert.Equal(paymentCompletedEvent.ProviderTransactionId, refundConsume.Context.Message.ProviderTransactionId);
        Assert.Equal(submittedEvent.TotalAmount, refundConsume.Context.Message.Amount);
        Assert.Equal(commitFailedEvent.Reason, refundConsume.Context.Message.Reason);

        var cancelShippingConsume = await _harness.Published
            .SelectAsync<CancelShippingCommand>()
            .FirstOrDefault();

        Assert.NotNull(cancelShippingConsume);
        Assert.Equal(submittedEvent.OrderId, cancelShippingConsume.Context.Message.OrderId);
        Assert.Equal(submittedEvent.OrderId, cancelShippingConsume.Context.Message.ShippingId);

        var cancelOrderConsume = await _harness.Published
            .SelectAsync<CancelOrderCommand>()
            .FirstOrDefault();

        Assert.NotNull(cancelOrderConsume);
        Assert.Equal(submittedEvent.OrderId, cancelOrderConsume.Context.Message.OrderId);
        Assert.Equal(submittedEvent.CustomerName, cancelOrderConsume.Context.Message.CustomerName);
        Assert.Equal(submittedEvent.CustomerEmail, cancelOrderConsume.Context.Message.CustomerEmail);
        Assert.Equal(commitFailedEvent.Reason, cancelOrderConsume.Context.Message.Reason);
    }

    [Theory]
    [AutoData]
    public async Task OnOrderReadyForPickupEvent_ShouldPublishConfirmShippingCommand(
        OrderSubmittedEvent submittedEvent,
        OrderReadyForPickupEvent readyEvent)
    {
        await _harness.InputQueueSendEndpoint.Send(submittedEvent);

        Assert.True(await _harness.Consumed.Any<OrderSubmittedEvent>());

        var submittedId = await _sagaHarness.Exists(submittedEvent.OrderId, _machine.Submitted);
        Assert.NotNull(submittedId);

        readyEvent = readyEvent with
        {
            OrderId = submittedEvent.OrderId
        };

        await _harness.InputQueueSendEndpoint.Send(readyEvent);

        Assert.True(await _harness.Consumed.Any<OrderReadyForPickupEvent>());
        Assert.True(await _harness.Published.Any<ConfirmPickupCommand>());

        var confirmConsume = await _harness.Published
            .SelectAsync<ConfirmPickupCommand>()
            .FirstOrDefault();

        Assert.NotNull(confirmConsume);
        Assert.Equal(submittedEvent.OrderId, confirmConsume.Context.Message.OrderId);
        Assert.Equal(submittedEvent.OrderId, confirmConsume.Context.Message.ShippingId);
        Assert.Equal(readyEvent.ReadyAt, confirmConsume.Context.Message.ReadyAt);
    }

    [Theory]
    [AutoData]
    public async Task OnShippingScheduledEvent_ShouldPublishUpdateOrderStatusCommandWithTrackingInfo(
        OrderSubmittedEvent submittedEvent,
        ShippingScheduledEvent scheduledEvent)
    {
        // Arrange
        await _harness.InputQueueSendEndpoint.Send(submittedEvent);

        Assert.True(await _harness.Consumed.Any<OrderSubmittedEvent>());

        var submittedId = await _sagaHarness.Exists(submittedEvent.OrderId, _machine.Submitted);
        Assert.NotNull(submittedId);

        // Act
        scheduledEvent = scheduledEvent with
        {
            OrderId = submittedEvent.OrderId
        };

        await _harness.InputQueueSendEndpoint.Send(scheduledEvent);

        // Assert
        Assert.True(await _harness.Consumed.Any<ShippingScheduledEvent>());
        Assert.True(await _harness.Published.Any<UpdateOrderStatusCommand>());

        // Find the UpdateOrderStatusCommand with ShippingStatus = "Scheduled"
        var updateCommands = await _harness.Published
            .SelectAsync<UpdateOrderStatusCommand>()
            .ToListAsync();

        var scheduledUpdate = updateCommands
            .Select(x => x.Context.Message)
            .FirstOrDefault(x => x.ShippingStatus == "Scheduled");

        Assert.NotNull(scheduledUpdate);
        Assert.Equal(submittedEvent.OrderId, scheduledUpdate.OrderId);
        Assert.Equal(scheduledEvent.TrackingNumber, scheduledUpdate.TrackingNumber);
        Assert.Equal(scheduledEvent.Carrier, scheduledUpdate.Carrier);
    }
}