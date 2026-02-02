using System.Linq;
using AutoFixture.Xunit2;
using Application.Saga;
using Common.Domain.Commands.Orders;
using Common.Domain.Events.Orders;
using Common.Domain.Events.Payments;
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
    public async Task OnOrderSubmitted_WhenPaymentIsCompleted_ShouldCompleteOrderAndPublishEvent(
        OrderSubmittedEvent submittedEvent,
        PaymentCompletedEvent paymentCompletedEvent)
    {
        // Act: send OrderSubmittedEvent
        await _harness.InputQueueSendEndpoint.Send(submittedEvent);

        Assert.True(await _harness.Consumed.Any<OrderSubmittedEvent>());

        // Assert saga instance created and in Submitted state
        var submittedId = await _sagaHarness.Exists(submittedEvent.OrderId, _machine.Submitted);
        Assert.NotNull(submittedId);

        // Act: simulate payment completed
        paymentCompletedEvent.OrderId = submittedEvent.OrderId;
        await _harness.InputQueueSendEndpoint.Send(paymentCompletedEvent);

        Assert.True(await _harness.Consumed.Any<PaymentCompletedEvent>());

        // Assert event published
        Assert.True(await _harness.Published.Any<OrderCompletedEvent>());

        var publishedConsume = await _harness.Published
            .SelectAsync<OrderCompletedEvent>()
            .FirstOrDefault();

        Assert.NotNull(publishedConsume);

        var published = publishedConsume.Context.Message;

        Assert.Equal(submittedEvent.OrderId, published.OrderId);
        Assert.Equal(submittedEvent.CustomerEmail, published.CustomerEmail);
        Assert.Equal(submittedEvent.CustomerName, published.CustomerName);
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
}