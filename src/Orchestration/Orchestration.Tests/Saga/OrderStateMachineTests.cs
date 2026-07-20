using Application.Saga;
using ValidationContext = System.ComponentModel.DataAnnotations.ValidationContext;
using ValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;
using Validator = System.ComponentModel.DataAnnotations.Validator;
using Domain.Common.Commands.Basket;
using Domain.Common.Commands.Orders;
using Domain.Common.Commands.Shipping;
using Domain.Common.Commands.Stock;
using Domain.Common.Events.Orders;
using Domain.Common.Events.Payments;
using Domain.Common.Events.Shipping;
using Domain.Common.Events.Stock;
using Domain.Common.States;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Orders.Tests.Orchestration.Saga;

public sealed class OrderStateMachineTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 7, 20, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan PaymentTimeout = TimeSpan.FromMinutes(7);

    private ITestHarness _harness = null!;
    private ISagaStateMachineTestHarness<OrderStateMachine, OrderState> _sagaHarness = null!;
    private OrderStateMachine _machine = null!;
    private ServiceProvider _provider = null!;

    public async Task InitializeAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging(logging => logging.SetMinimumLevel(LogLevel.Warning));
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IOptions<OrderSagaOptions>>(
            Options.Create(new OrderSagaOptions { PaymentTimeout = PaymentTimeout }));
        services.AddMassTransitTestHarness(configurator =>
        {
            configurator.AddSagaStateMachine<OrderStateMachine, OrderState>()
                .InMemoryRepository();
        });

        _provider = services.BuildServiceProvider(true);
        _harness = _provider.GetRequiredService<ITestHarness>();
        _machine = _provider.GetRequiredService<OrderStateMachine>();
        _sagaHarness = _harness.GetSagaStateMachineHarness<OrderStateMachine, OrderState>();
        await _harness.Start();
    }

    public async Task DisposeAsync()
    {
        await _harness.Stop();
        await _provider.DisposeAsync();
    }

    [Fact]
    public async Task Timeout_before_payment_cancels_once_and_becomes_terminal()
    {
        var orderId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        await Submit(orderId);

        await _harness.Bus.Publish(Expired(orderId));

        Assert.NotNull(await _sagaHarness.Exists(orderId, _machine.Failed));
        Assert.Equal(1, await SentCount<CancelOrderCommand>(orderId));
        Assert.Equal(1, await SentCount<ReleaseStockReservationCommand>(orderId));

        await _harness.Bus.Publish(Payment(orderId));

        Assert.NotNull(await _sagaHarness.Exists(orderId, _machine.Failed));
        Assert.Equal(0, await SentCount<CommitStockReservationCommand>(orderId));
    }

    [Fact]
    public async Task Payment_before_timeout_exits_payment_pending_and_never_cancels()
    {
        var orderId = Guid.Parse("10000000-0000-0000-0000-000000000002");
        await Submit(orderId);

        await _harness.Bus.Publish(Payment(orderId));

        Assert.NotNull(await _sagaHarness.Exists(orderId, _machine.Processing));
        Assert.Equal(1, await SentCount<CommitStockReservationCommand>(orderId));
        Assert.Equal(1, await SentCount<EmptyBasketCommand>(orderId));
        Assert.Equal(1, await SentCount<ScheduleShippingCommand>(orderId));
        Assert.Equal(0, await SentCount<CancelOrderCommand>(orderId));
    }

    [Fact]
    public async Task Stale_timeout_after_payment_is_ignored()
    {
        var orderId = Guid.Parse("10000000-0000-0000-0000-000000000003");
        await Submit(orderId);
        await _harness.Bus.Publish(Payment(orderId));

        await _harness.Bus.Publish(Expired(orderId));

        Assert.NotNull(await _sagaHarness.Exists(orderId, _machine.Processing));
        Assert.Equal(0, await SentCount<CancelOrderCommand>(orderId));
        Assert.Equal(0, await SentCount<ReleaseStockReservationCommand>(orderId));
    }

    [Fact]
    public async Task Duplicate_payment_completed_does_not_repeat_business_effects()
    {
        var orderId = Guid.Parse("10000000-0000-0000-0000-000000000004");
        var payment = Payment(orderId);
        await Submit(orderId);

        await _harness.Bus.Publish(payment);
        await _harness.Bus.Publish(payment);

        Assert.NotNull(await _sagaHarness.Exists(orderId, _machine.Processing));
        Assert.Equal(1, await SentCount<CommitStockReservationCommand>(orderId));
        Assert.Equal(1, await SentCount<EmptyBasketCommand>(orderId));
        Assert.Equal(1, await SentCount<ScheduleShippingCommand>(orderId));
        Assert.Equal(1, await StatusCount(orderId, "Paid"));
    }

    [Fact]
    public async Task Duplicate_shipping_and_delivery_events_do_not_repeat_effects()
    {
        var orderId = Guid.Parse("10000000-0000-0000-0000-000000000005");
        var shipping = ShippingScheduled(orderId);
        var stock = StockCommitted(orderId);
        var shipped = Shipped(orderId);
        var delivered = Delivered(orderId);
        await Submit(orderId);
        await _harness.Bus.Publish(Payment(orderId));

        await _harness.Bus.Publish(shipping);
        await _harness.Bus.Publish(shipping);
        await _harness.Bus.Publish(stock);
        await _harness.Bus.Publish(stock);
        await _harness.Bus.Publish(shipped);
        await _harness.Bus.Publish(shipped);
        await _harness.Bus.Publish(delivered);
        await _harness.Bus.Publish(delivered);

        Assert.NotNull(await _sagaHarness.Exists(orderId, _machine.Completed));
        Assert.Equal(1, await StatusCount(orderId, "Shipped"));
        Assert.Equal(1, await StatusCount(orderId, "Delivered"));
        Assert.Equal(1, await StatusCount(orderId, "Processing"));
        Assert.Equal(1, await _harness.Published.SelectAsync<CompleteOrderCommand>()
            .CountAsync(x => x.Context.Message.OrderId == orderId));
    }

    [Fact]
    public async Task Payment_timeout_race_commits_only_one_business_outcome()
    {
        for (var index = 0; index < 12; index++)
        {
            var orderId = Guid.Parse($"20000000-0000-0000-0000-{index + 1:000000000000}");
            await Submit(orderId);

            await Task.WhenAll(
                _harness.Bus.Publish(Payment(orderId)),
                _harness.Bus.Publish(Expired(orderId)));

            var settledState = await WaitForSettledState(orderId);
            var paidEffects = await SentCount<CommitStockReservationCommand>(orderId);
            var cancelledEffects = await SentCount<CancelOrderCommand>(orderId);
            Assert.True((paidEffects, cancelledEffects) is (1, 0) or (0, 1));
            Assert.True(settledState is "Processing" or "Failed");
        }
    }

    [Fact]
    public async Task Terminal_saga_cannot_be_reactivated()
    {
        var orderId = Guid.Parse("10000000-0000-0000-0000-000000000006");
        await Complete(orderId);
        var completedEffectsBeforeReplay =
            await StatusCount(orderId, "Delivered") +
            await SentCount<CommitStockReservationCommand>(orderId) +
            await SentCount<CancelOrderCommand>(orderId);

        await _harness.Bus.Publish(Submitted(orderId));
        await _harness.Bus.Publish(Payment(orderId));
        await _harness.Bus.Publish(Expired(orderId));
        await _harness.Bus.Publish(Shipped(orderId));

        Assert.NotNull(await _sagaHarness.Exists(orderId, _machine.Completed));
        Assert.Equal(
            completedEffectsBeforeReplay,
            await StatusCount(orderId, "Delivered") +
            await SentCount<CommitStockReservationCommand>(orderId) +
            await SentCount<CancelOrderCommand>(orderId));
    }

    [Fact]
    public async Task Timeout_delay_and_expired_at_share_the_validated_option()
    {
        var orderId = Guid.Parse("10000000-0000-0000-0000-000000000007");
        var before = DateTime.UtcNow;

        await Submit(orderId);
        var after = DateTime.UtcNow;

        var scheduled = await _harness.Sent.SelectAsync<OrderExpiredEvent>()
            .FirstAsync(x => x.Context.Message.OrderId == orderId);
        Assert.InRange(scheduled.Context.Message.ExpiredAt, before.Add(PaymentTimeout), after.Add(PaymentTimeout));
        Assert.NotNull(scheduled.Context.ScheduledMessageId);
    }

    [Fact]
    public void Payment_timeout_option_rejects_non_positive_duration()
    {
        var options = new OrderSagaOptions { PaymentTimeout = TimeSpan.Zero };
        var validationResults = new List<ValidationResult>();

        Assert.False(Validator.TryValidateObject(
            options,
            new ValidationContext(options),
            validationResults,
            validateAllProperties: true));
        Assert.Contains(validationResults, result =>
            result.MemberNames.Contains(nameof(OrderSagaOptions.PaymentTimeout)));
    }

    [Fact]
    public async Task Saga_produced_commands_have_stable_correlation_and_business_identifiers()
    {
        var orderId = Guid.Parse("10000000-0000-0000-0000-000000000008");
        var payment = Payment(orderId);
        var submitted = Submitted(orderId);
        await _harness.Bus.Publish(submitted);

        await _harness.Bus.Publish(payment);

        var commit = await FirstSent<CommitStockReservationCommand>(orderId);
        var basket = await FirstSent<EmptyBasketCommand>(orderId);
        Assert.Equal(orderId, commit.CorrelationId);
        Assert.Equal(submitted.ReservationId, commit.ReservationId);
        Assert.Equal(orderId, basket.CorrelationId);
        Assert.Equal(submitted.BasketClientId, basket.ClientId);
        Assert.NotEqual(Guid.Empty, commit.EventId);
        Assert.NotEqual(payment.EventId, commit.EventId);

        var otherOrderId = Guid.Parse("10000000-0000-0000-0000-000000000009");
        await _harness.Bus.Publish(Submitted(otherOrderId));
        await _harness.Bus.Publish(Payment(otherOrderId));
        var otherCommit = await FirstSent<CommitStockReservationCommand>(otherOrderId);
        Assert.NotEqual(commit.EventId, otherCommit.EventId);
    }

    private async Task Submit(Guid orderId)
    {
        await _harness.Bus.Publish(Submitted(orderId));
        Assert.NotNull(await _sagaHarness.Exists(orderId, _machine.Submitted));
    }

    private async Task Complete(Guid orderId)
    {
        await Submit(orderId);
        await _harness.Bus.Publish(Payment(orderId));
        await _harness.Bus.Publish(ShippingScheduled(orderId));
        await _harness.Bus.Publish(StockCommitted(orderId));
        await _harness.Bus.Publish(Shipped(orderId));
        await _harness.Bus.Publish(Delivered(orderId));
        Assert.NotNull(await _sagaHarness.Exists(orderId, _machine.Completed));
    }

    private static OrderSubmittedEvent Submitted(Guid orderId) => new()
    {
        EventId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
        OrderId = orderId,
        CustomerId = Guid.Parse("30000000-0000-0000-0000-000000000001"),
        BasketClientId = Guid.Parse("30000000-0000-0000-0000-000000000002"),
        PaymentId = Guid.Parse("30000000-0000-0000-0000-000000000003"),
        ReservationId = Guid.Parse("30000000-0000-0000-0000-000000000004"),
        CustomerName = "Test Customer",
        CustomerEmail = "test@example.invalid",
        TotalAmount = 125.50m,
        DestinationAddress = "Test address"
    };

    private static PaymentCompletedEvent Payment(Guid orderId) => new()
    {
        EventId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
        OrderId = orderId,
        PaymentId = Guid.Parse("30000000-0000-0000-0000-000000000003"),
        ProviderTransactionId = "provider-transaction-1",
        PSPTransactionId = "psp-transaction-1",
        Amount = 125.50m,
        Currency = "USD"
    };

    private static OrderExpiredEvent Expired(Guid orderId) => new()
    {
        EventId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
        OrderId = orderId,
        ExpiredAt = Now.Add(PaymentTimeout).UtcDateTime
    };

    private static ShippingScheduledEvent ShippingScheduled(Guid orderId) => new()
    {
        EventId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
        OrderId = orderId,
        ShipmentId = Guid.Parse("30000000-0000-0000-0000-000000000005"),
        Carrier = "Test carrier",
        TrackingNumber = "TRACK-1",
        DestinationAddress = "Test address"
    };

    private static StockReservationCommittedEvent StockCommitted(Guid orderId) => new()
    {
        EventId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
        OrderId = orderId,
        ReservationId = Guid.Parse("30000000-0000-0000-0000-000000000004"),
        CommittedAt = Now.UtcDateTime
    };

    private static OrderShippedEvent Shipped(Guid orderId) => new()
    {
        EventId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"),
        OrderId = orderId,
        Carrier = "Test carrier",
        TrackingNumber = "TRACK-1",
        ShippedAt = Now.AddHours(1).UtcDateTime
    };

    private static OrderDeliveredEvent Delivered(Guid orderId) => new()
    {
        EventId = Guid.Parse("99999999-9999-9999-9999-999999999999"),
        OrderId = orderId,
        TrackingNumber = "TRACK-1",
        DeliveredAt = Now.AddDays(1).UtcDateTime
    };

    private async Task<int> StatusCount(Guid orderId, string status) =>
        await _harness.Sent.SelectAsync<UpdateOrderStatusCommand>()
            .CountAsync(x => x.Context.Message.OrderId == orderId && x.Context.Message.Status == status);

    private async Task<int> SentCount<T>(Guid orderId)
        where T : class =>
        await _harness.Sent.SelectAsync<T>()
            .CountAsync(x => x.Context.Message is Domain.Common.Commands.BaseCommand command && command.OrderId == orderId);

    private async Task<T> FirstSent<T>(Guid orderId)
        where T : class
    {
        var message = await _harness.Sent.SelectAsync<T>()
            .FirstAsync(x => x.Context.Message is Domain.Common.Commands.BaseCommand command && command.OrderId == orderId);
        return message.Context.Message;
    }

    private async Task<string> WaitForSettledState(Guid orderId)
    {
        for (var attempt = 0; attempt < 200; attempt++)
        {
            var state = _sagaHarness.Sagas.Contains(orderId)?.CurrentState;
            if (state is "Processing" or "Failed")
            {
                return state;
            }

            await Task.Delay(10);
        }

        return _sagaHarness.Sagas.Contains(orderId)?.CurrentState ?? string.Empty;
    }
}
