using System.Linq;
using AutoFixture.Xunit2;
using Application.Saga;
using Domain.Common.Commands.Basket;
using Domain.Common.Commands.Operations;
using Domain.Common.Commands.Orders;
using Domain.Common.Commands.Payments;
using Domain.Common.Commands.Shipping;
using Domain.Common.Commands.Stock;
using Domain.Common.Events.Operations;
using Domain.Common.Events.Orders;
using Domain.Common.Events.Payments;
using Domain.Common.Events.Shipping;
using Domain.Common.Events.Stock;
using Domain.Common.States;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Orders.Tests.Orchestration.Saga;

public class OrderStateMachineTests : IAsyncLifetime
{
    private ITestHarness _harness = null!;
    private ServiceProvider _provider = null!;

    public async Task InitializeAsync()
    {
        // Arrange: setup DI and test harness
        _provider = new ServiceCollection()
            .AddMassTransitTestHarness(cfg =>
            {
                cfg.AddSagaStateMachine<OrderStateMachine, OrderState>()
                    .InMemoryRepository();
            })
            .BuildServiceProvider(true);

        _harness = _provider.GetRequiredService<ITestHarness>();

        await _harness.Start();
    }

    public async Task DisposeAsync()
    {
        if (_harness != null)
        {
            await _harness.Stop();
        }

        if (_provider != null)
        {
            await _provider.DisposeAsync();
        }
    }

    [Theory]
    [AutoData]
    public async Task OnPaymentCompleted_ShouldPublishStockCommitEmptyBasketAndShippingCommands(
        OrderSubmittedEvent submittedEvent,
        PaymentCompletedEvent paymentCompletedEvent)
    {
        // Arrange
        var sagaHarness = _harness.GetSagaStateMachineHarness<OrderStateMachine, OrderState>();

        // Act
        await _harness.Bus.Publish(submittedEvent);

        // Assert: OrderSubmittedEvent consumed and saga created
        Assert.True(await _harness.Consumed.Any<OrderSubmittedEvent>());

        var saga = sagaHarness.Sagas.Contains(submittedEvent.OrderId);
        Assert.NotNull(saga);

        // Act: send PaymentCompletedEvent
        paymentCompletedEvent.OrderId = submittedEvent.OrderId;
        await _harness.Bus.Publish(paymentCompletedEvent);

        // Assert: PaymentCompletedEvent consumed
        Assert.True(await _harness.Consumed.Any<PaymentCompletedEvent>());

        // Assert: commands sent to deterministic endpoints
        Assert.True(await _harness.Sent.Any<CommitStockReservationCommand>());
        Assert.True(await _harness.Sent.Any<EmptyBasketCommand>());
        Assert.True(await _harness.Sent.Any<ScheduleShippingCommand>());

        await AssertSentTo<CommitStockReservationCommand>("commit-stock-reservation");
        await AssertSentTo<EmptyBasketCommand>("empty-basket");
        await AssertSentTo<ScheduleShippingCommand>("schedule-shipping");
        await AssertSentTo<UpdateOrderStatusCommand>("update-order-status-command");

        var commitConsume = await _harness.Sent
            .SelectAsync<CommitStockReservationCommand>()
            .FirstOrDefault();

        Assert.NotNull(commitConsume);
        Assert.Equal(submittedEvent.OrderId, commitConsume.Context.Message.OrderId);
        Assert.Equal(submittedEvent.ReservationId, commitConsume.Context.Message.ReservationId);

        var emptyBasketConsume = await _harness.Sent
            .SelectAsync<EmptyBasketCommand>()
            .FirstOrDefault();

        Assert.NotNull(emptyBasketConsume);
        Assert.Equal(submittedEvent.OrderId, emptyBasketConsume.Context.Message.OrderId);
        Assert.Equal(submittedEvent.BasketClientId, emptyBasketConsume.Context.Message.ClientId);

        var scheduleConsume = await _harness.Sent
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
        // Arrange
        var sagaHarness = _harness.GetSagaStateMachineHarness<OrderStateMachine, OrderState>();

        // Act
        await _harness.Bus.Publish(submittedEvent);

        // Assert: OrderSubmittedEvent consumed and saga created
        Assert.True(await _harness.Consumed.Any<OrderSubmittedEvent>());

        var saga = sagaHarness.Sagas.Contains(submittedEvent.OrderId);
        Assert.NotNull(saga);

        // Act: payment failed
        paymentFailedEvent.OrderId = submittedEvent.OrderId;
        await _harness.Bus.Publish(paymentFailedEvent);

        // Assert
        Assert.True(await _harness.Consumed.Any<PaymentFailedEvent>());

        // Assert CancelOrderCommand was published with reason + customer data
        Assert.True(await _harness.Sent.Any<CancelOrderCommand>());

        await AssertSentTo<ReleaseStockReservationCommand>("release-stock-reservation");
        await AssertSentTo<UpdateOrderStatusCommand>("update-order-status-command");
        await AssertSentTo<CancelOrderCommand>("cancel-order-command");

        var cancelConsume = await _harness.Sent
            .SelectAsync<CancelOrderCommand>()
            .FirstOrDefault();

        Assert.NotNull(cancelConsume);

        var cancel = cancelConsume.Context.Message;

        Assert.Equal(submittedEvent.OrderId, cancel.OrderId);
        Assert.Equal($"Payment failed: {paymentFailedEvent.Reason}", cancel.Reason);
        Assert.False(string.IsNullOrWhiteSpace(cancel.CustomerEmail));
        Assert.False(string.IsNullOrWhiteSpace(cancel.CustomerName));
    }


    [Theory]
    [AutoData]
    public async Task OnStockReservationCommitted_ShouldPublishPreparePackageCommand(
        OrderSubmittedEvent submittedEvent,
        StockReservationCommittedEvent committedEvent)
    {
        // Arrange
        var sagaHarness = _harness.GetSagaStateMachineHarness<OrderStateMachine, OrderState>();

        // Act
        await _harness.Bus.Publish(submittedEvent);

        // Assert: OrderSubmittedEvent consumed and saga created
        Assert.True(await _harness.Consumed.Any<OrderSubmittedEvent>());

        var saga = sagaHarness.Sagas.Contains(submittedEvent.OrderId);
        Assert.NotNull(saga);

        // Act: StockReservationCommittedEvent
        committedEvent = committedEvent with
        {
            OrderId = submittedEvent.OrderId,
            ReservationId = submittedEvent.ReservationId
        };
        await _harness.Bus.Publish(committedEvent);

        // Assert
        Assert.True(await _harness.Consumed.Any<StockReservationCommittedEvent>());
        Assert.True(await _harness.Sent.Any<PreparePackageCommand>());

        await AssertSentTo<PreparePackageCommand>("prepare-package");
        await AssertSentTo<UpdateOrderStatusCommand>("update-order-status-command");

        var prepareConsume = await _harness.Sent
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
        // Arrange
        var sagaHarness = _harness.GetSagaStateMachineHarness<OrderStateMachine, OrderState>();

        // Act
        await _harness.Bus.Publish(submittedEvent);

        // Assert: OrderSubmittedEvent consumed and saga created
        Assert.True(await _harness.Consumed.Any<OrderSubmittedEvent>());

        var saga = sagaHarness.Sagas.Contains(submittedEvent.OrderId);
        Assert.NotNull(saga);

        // Act: PaymentCompletedEvent
        paymentCompletedEvent.OrderId = submittedEvent.OrderId;
        await _harness.Bus.Publish(paymentCompletedEvent);

        Assert.True(await _harness.Consumed.Any<PaymentCompletedEvent>());

        // Act: StockReservationCommitFailedEvent
        commitFailedEvent.OrderId = submittedEvent.OrderId;
        await _harness.Bus.Publish(commitFailedEvent);

        // Assert
        Assert.True(await _harness.Consumed.Any<StockReservationCommitFailedEvent>());
        Assert.True(await _harness.Sent.Any<RefundPaymentCommand>());
        Assert.True(await _harness.Sent.Any<CancelShippingCommand>());
        Assert.True(await _harness.Sent.Any<CancelOrderCommand>());

        await AssertSentTo<RefundPaymentCommand>("refund-payment");
        await AssertSentTo<CancelShippingCommand>("cancel-shipping");
        await AssertSentTo<UpdateOrderStatusCommand>("update-order-status-command");
        await AssertSentTo<CancelOrderCommand>("cancel-order-command");

        var refundConsume = await _harness.Sent
            .SelectAsync<RefundPaymentCommand>()
            .FirstOrDefault();

        Assert.NotNull(refundConsume);
        Assert.Equal(submittedEvent.OrderId, refundConsume.Context.Message.OrderId);
        Assert.Equal(paymentCompletedEvent.PaymentId, refundConsume.Context.Message.PaymentId);
        Assert.Equal(paymentCompletedEvent.ProviderTransactionId, refundConsume.Context.Message.ProviderTransactionId);
        Assert.Equal(submittedEvent.TotalAmount, refundConsume.Context.Message.Amount);
        Assert.Equal(commitFailedEvent.Reason, refundConsume.Context.Message.Reason);

        var cancelShippingConsume = await _harness.Sent
            .SelectAsync<CancelShippingCommand>()
            .FirstOrDefault();

        Assert.NotNull(cancelShippingConsume);
        Assert.Equal(submittedEvent.OrderId, cancelShippingConsume.Context.Message.OrderId);
        Assert.Equal(submittedEvent.OrderId, cancelShippingConsume.Context.Message.ShippingId);

        var cancelOrderConsume = await _harness.Sent
            .SelectAsync<CancelOrderCommand>()
            .FirstOrDefault();

        Assert.NotNull(cancelOrderConsume);
        Assert.Equal(submittedEvent.OrderId, cancelOrderConsume.Context.Message.OrderId);
        Assert.Equal(submittedEvent.CustomerName, cancelOrderConsume.Context.Message.CustomerName);
        Assert.Equal(submittedEvent.CustomerEmail, cancelOrderConsume.Context.Message.CustomerEmail);
        Assert.Equal($"Stock reservation failed: {commitFailedEvent.Reason}", cancelOrderConsume.Context.Message.Reason);
    }

    [Theory]
    [AutoData]
    public async Task OnOrderReadyForPickupEvent_ShouldPublishConfirmShippingCommand(
        OrderSubmittedEvent submittedEvent,
        ShippingScheduledEvent scheduledEvent,
        OrderReadyForPickupEvent readyEvent)
    {
        // Arrange
        var sagaHarness = _harness.GetSagaStateMachineHarness<OrderStateMachine, OrderState>();

        // Act
        await _harness.Bus.Publish(submittedEvent);

        // Assert: OrderSubmittedEvent consumed and saga created
        Assert.True(await _harness.Consumed.Any<OrderSubmittedEvent>());

        var saga = sagaHarness.Sagas.Contains(submittedEvent.OrderId);
        Assert.NotNull(saga);

        // The provider creates its own shipment identifier. Preserve it separately
        // from the order correlation identifier before confirming pickup.
        scheduledEvent = scheduledEvent with
        {
            OrderId = submittedEvent.OrderId
        };

        await _harness.Bus.Publish(scheduledEvent);

        Assert.True(await _harness.Consumed.Any<ShippingScheduledEvent>());
        saga = sagaHarness.Sagas.Contains(submittedEvent.OrderId);
        Assert.NotNull(saga);
        Assert.Equal(submittedEvent.OrderId, saga.CorrelationId);
        Assert.Equal(scheduledEvent.ShipmentId, saga.ShipmentId);

        // Act: OrderReadyForPickupEvent
        readyEvent = readyEvent with
        {
            OrderId = submittedEvent.OrderId
        };

        await _harness.Bus.Publish(readyEvent);

        // Assert
        Assert.True(await _harness.Consumed.Any<OrderReadyForPickupEvent>());
        Assert.True(await _harness.Sent.Any<ConfirmPickupCommand>());

        await AssertSentTo<ConfirmPickupCommand>("confirm-shipping");
        await AssertSentTo<UpdateOrderStatusCommand>("update-order-status-command");

        var confirmConsume = await _harness.Sent
            .SelectAsync<ConfirmPickupCommand>()
            .FirstOrDefault();

        Assert.NotNull(confirmConsume);
        Assert.Equal(submittedEvent.OrderId, confirmConsume.Context.Message.OrderId);
        Assert.Equal(scheduledEvent.ShipmentId, confirmConsume.Context.Message.ShippingId);
        Assert.NotEqual(confirmConsume.Context.Message.OrderId, confirmConsume.Context.Message.ShippingId);
        Assert.Equal(readyEvent.ReadyAt, confirmConsume.Context.Message.ReadyAt);
    }

    [Theory]
    [AutoData]
    public async Task OnShippingScheduledEvent_ShouldPublishUpdateOrderStatusCommandWithTrackingInfo(
        OrderSubmittedEvent submittedEvent,
        ShippingScheduledEvent scheduledEvent)
    {
        // Arrange
        var sagaHarness = _harness.GetSagaStateMachineHarness<OrderStateMachine, OrderState>();

        // Act
        await _harness.Bus.Publish(submittedEvent);

        // Assert: OrderSubmittedEvent consumed and saga created
        Assert.True(await _harness.Consumed.Any<OrderSubmittedEvent>());

        var saga = sagaHarness.Sagas.Contains(submittedEvent.OrderId);
        Assert.NotNull(saga);

        // Act
        scheduledEvent = scheduledEvent with
        {
            OrderId = submittedEvent.OrderId
        };

        await _harness.Bus.Publish(scheduledEvent);

        // Assert
        Assert.True(await _harness.Consumed.Any<ShippingScheduledEvent>());
        Assert.True(await _harness.Sent.Any<UpdateOrderStatusCommand>());

        await AssertSentTo<UpdateOrderStatusCommand>("update-order-status-command");

        // Find the UpdateOrderStatusCommand with ShippingStatus = "Scheduled"
        var updateCommands = await _harness.Sent
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

    [Theory]
    [AutoData]
    public async Task OnOrderSubmitted_ShouldSchedulePaymentTimeout(
        OrderSubmittedEvent submittedEvent)
    {
        await _harness.Bus.Publish(submittedEvent);

        var scheduled = await _harness.Sent
            .SelectAsync<OrderExpiredEvent>()
            .FirstOrDefault();

        Assert.NotNull(scheduled);
        Assert.Equal(submittedEvent.OrderId, scheduled.Context.Message.OrderId);
        Assert.InRange(
            scheduled.Context.Delay!.Value,
            TimeSpan.FromMinutes(4.99),
            TimeSpan.FromMinutes(5));
        Assert.NotNull(scheduled.Context.ScheduledMessageId);
    }

    [Theory]
    [AutoData]
    public async Task OnPaymentTimeout_ShouldSendCancellationCommandsToDeterministicEndpoints(
        OrderSubmittedEvent submittedEvent,
        OrderExpiredEvent expiredEvent)
    {
        await _harness.Bus.Publish(submittedEvent);

        expiredEvent = expiredEvent with { OrderId = submittedEvent.OrderId };
        await _harness.Bus.Publish(expiredEvent);

        await AssertSentTo<ReleaseStockReservationCommand>("release-stock-reservation");
        await AssertSentTo<UpdateOrderStatusCommand>("update-order-status-command");
        await AssertSentTo<CancelOrderCommand>("cancel-order-command");
    }

    [Theory]
    [AutoData]
    public async Task OnPackageIssueReported_ShouldSendAllCompensationCommandsToDeterministicEndpoints(
        OrderSubmittedEvent submittedEvent,
        PaymentCompletedEvent paymentCompletedEvent,
        PackageIssueReportedEvent packageIssue)
    {
        await _harness.Bus.Publish(submittedEvent);

        paymentCompletedEvent.OrderId = submittedEvent.OrderId;
        await _harness.Bus.Publish(paymentCompletedEvent);

        packageIssue = packageIssue with { OrderId = submittedEvent.OrderId };
        await _harness.Bus.Publish(packageIssue);

        await AssertSentTo<RefundPaymentCommand>("refund-payment");
        await AssertSentTo<ReleaseStockReservationCommand>("release-stock-reservation");
        await AssertSentTo<CancelShippingCommand>("cancel-shipping");
        await AssertSentTo<UpdateOrderStatusCommand>("update-order-status-command");
        await AssertSentTo<CancelOrderCommand>("cancel-order-command");
    }

    [Theory]
    [AutoData]
    public async Task OnShippingFailedBeforeDispatch_ShouldSendCompensationCommandsToDeterministicEndpoints(
        OrderSubmittedEvent submittedEvent,
        PaymentCompletedEvent paymentCompletedEvent,
        ShippingFailedEvent shippingFailed)
    {
        await _harness.Bus.Publish(submittedEvent);

        paymentCompletedEvent.OrderId = submittedEvent.OrderId;
        await _harness.Bus.Publish(paymentCompletedEvent);

        shippingFailed = shippingFailed with { OrderId = submittedEvent.OrderId };
        await _harness.Bus.Publish(shippingFailed);

        await AssertSentTo<RefundPaymentCommand>("refund-payment");
        await AssertSentTo<ReleaseStockReservationCommand>("release-stock-reservation");
        await AssertSentTo<UpdateOrderStatusCommand>("update-order-status-command");
        await AssertSentTo<CancelOrderCommand>("cancel-order-command");
    }

    [Theory]
    [AutoData]
    public async Task OnOrderDelivered_ShouldPublishIntentionalCompletionExceptionAndSendStatusUpdate(
        OrderSubmittedEvent submittedEvent,
        OrderShippedEvent shippedEvent,
        OrderDeliveredEvent deliveredEvent)
    {
        await _harness.Bus.Publish(submittedEvent);

        shippedEvent = shippedEvent with { OrderId = submittedEvent.OrderId };
        await _harness.Bus.Publish(shippedEvent);

        deliveredEvent = deliveredEvent with { OrderId = submittedEvent.OrderId };
        await _harness.Bus.Publish(deliveredEvent);

        Assert.True(await _harness.Published.Any<CompleteOrderCommand>());
        await AssertSentTo<UpdateOrderStatusCommand>("update-order-status-command");

        var updates = await _harness.Sent
            .SelectAsync<UpdateOrderStatusCommand>()
            .ToListAsync();
        var update = updates
            .Select(message => message.Context.Message)
            .Single(message => message.Status == "Delivered");

        Assert.Equal("Delivered", update.Status);
        Assert.Equal(deliveredEvent.TrackingNumber, update.TrackingNumber);
        Assert.Equal(deliveredEvent.DeliveredAt, update.DeliveredAt);
    }

    [Theory]
    [AutoData]
    public async Task OnShippingFailedAfterDispatch_ShouldSendPostDispatchCompensationToDeterministicEndpoints(
        OrderSubmittedEvent submittedEvent,
        OrderShippedEvent shippedEvent,
        ShippingFailedEvent shippingFailed)
    {
        await _harness.Bus.Publish(submittedEvent);

        shippedEvent = shippedEvent with { OrderId = submittedEvent.OrderId };
        await _harness.Bus.Publish(shippedEvent);

        shippingFailed = shippingFailed with { OrderId = submittedEvent.OrderId };
        await _harness.Bus.Publish(shippingFailed);

        await AssertSentTo<RefundPaymentCommand>("refund-payment");
        await AssertSentTo<UpdateOrderStatusCommand>("update-order-status-command");
        await AssertSentTo<CancelOrderCommand>("cancel-order-command");
    }

    private async Task<T> AssertSentTo<T>(string endpointName)
        where T : class
    {
        var sent = await _harness.Sent
            .SelectAsync<T>()
            .FirstOrDefault();

        Assert.NotNull(sent);
        Assert.NotNull(sent.Context.DestinationAddress);
        Assert.Equal(endpointName, sent.Context.DestinationAddress.Segments.Last().Trim('/'));
        return sent.Context.Message;
    }
}
