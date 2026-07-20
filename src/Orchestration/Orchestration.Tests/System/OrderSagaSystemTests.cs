using Domain.Common.Commands.Orders;
using Domain.Common.Commands.Stock;
using Domain.Common.Events.Orders;
using Domain.Common.Events.Payments;
using Domain.Common.Events.Shipping;
using Domain.Common.Events.Stock;
using Domain.Common.States;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Orchestration.Data;

namespace Orders.Tests.Orchestration.SystemTests;

[Collection(OrderSagaSystemCollection.Name)]
public sealed class OrderSagaSystemTests(OrderSagaSystemFixture fixture)
{
    private static readonly TimeSpan DefaultWait = TimeSpan.FromSeconds(20);

    [OrderSagaSystemFact]
    [Trait("Category", "System")]
    public async Task Persisted_timeout_survives_host_restart_and_cancels_unpaid_order()
    {
        await using var scope = await fixture.CreateScopeAsync();
        var effects = new EffectProbe();
        var orderId = Guid.NewGuid();
        var timeout = TimeSpan.FromSeconds(5);
        IHost? firstHost = null;
        IHost? restartedHost = null;

        try
        {
            firstHost = OrderSagaSystemHost.Create(scope, effects, timeout);
            await firstHost.StartAsync();
            await firstHost.Services.GetRequiredService<IBus>().Publish(Submitted(orderId));

            await WaitForStateAsync(scope, orderId, effects, "Submitted");
            await SystemTestWait.UntilAsync(
                async () => await QuartzTriggerCountAsync(scope) == 1,
                DefaultWait);

            await OrderSagaSystemHost.StopAsync(firstHost);
            firstHost = null;

            await Task.Delay(timeout.Add(TimeSpan.FromSeconds(1)));

            restartedHost = OrderSagaSystemHost.Create(scope, effects, timeout);
            await restartedHost.StartAsync();

            await WaitForStateAsync(scope, orderId, "Failed");
            await effects.WaitForCountAsync<CancelOrderCommand>(orderId, 1);
            Assert.Equal(1, effects.Count<CancelOrderCommand>(orderId));
            Assert.Equal(1, effects.Count<ReleaseStockReservationCommand>(orderId));
        }
        finally
        {
            if (firstHost is not null)
            {
                await OrderSagaSystemHost.StopAsync(firstHost);
            }

            if (restartedHost is not null)
            {
                await OrderSagaSystemHost.StopAsync(restartedHost);
            }
        }
    }

    [OrderSagaSystemFact]
    [Trait("Category", "System")]
    public async Task Duplicates_stale_timeout_and_terminal_replays_do_not_repeat_effects()
    {
        await using var scope = await fixture.CreateScopeAsync();
        var effects = new EffectProbe();
        var orderId = Guid.NewGuid();
        var submitted = Submitted(orderId);
        var payment = Payment(orderId);
        var shipping = ShippingScheduled(orderId);
        var stock = StockCommitted(orderId);
        var ready = ReadyForPickup(orderId);
        var shipped = Shipped(orderId);
        var delivered = Delivered(orderId);
        var host = OrderSagaSystemHost.Create(scope, effects, TimeSpan.FromSeconds(30));

        try
        {
            await host.StartAsync();
            var bus = host.Services.GetRequiredService<IBus>();

            await bus.Publish(submitted);
            await WaitForStateAsync(scope, orderId, "Submitted");

            await Task.WhenAll(bus.Publish(payment), bus.Publish(payment));
            await WaitForStateAsync(scope, orderId, "Processing");
            await effects.WaitForCountAsync<CommitStockReservationCommand>(orderId, 1);

            await Task.WhenAll(
                bus.Publish(Expired(orderId)),
                bus.Publish(Expired(orderId)),
                bus.Publish(payment));
            await AssertStateAsync(scope, orderId, "Processing");
            Assert.Equal(0, effects.Count<CancelOrderCommand>(orderId));
            Assert.Equal(1, effects.Count<CommitStockReservationCommand>(orderId));

            await Task.WhenAll(bus.Publish(shipping), bus.Publish(shipping));
            await Task.WhenAll(bus.Publish(stock), bus.Publish(stock));
            await WaitForStateAsync(scope, orderId, "Fulfilling");

            await Task.WhenAll(bus.Publish(ready), bus.Publish(ready));
            await WaitForStateAsync(scope, orderId, "ReadyForPickup");

            await Task.WhenAll(bus.Publish(shipped), bus.Publish(shipped));
            await WaitForStateAsync(scope, orderId, "Shipped");

            await Task.WhenAll(bus.Publish(delivered), bus.Publish(delivered));
            await WaitForStateAsync(scope, orderId, "Completed");
            await effects.WaitForCountAsync<CompleteOrderCommand>(orderId, 1);

            Assert.Equal(1, effects.Count<UpdateOrderStatusCommand>(
                orderId,
                x => x.Status == "Paid" && x.PaymentStatus == "Captured"));
            Assert.Equal(1, effects.Count<UpdateOrderStatusCommand>(
                orderId,
                x => x.ShippingStatus == "Scheduled"));
            Assert.Equal(1, effects.Count<UpdateOrderStatusCommand>(orderId, x => x.Status == "Shipped"));
            Assert.Equal(1, effects.Count<UpdateOrderStatusCommand>(orderId, x => x.Status == "Delivered"));
            Assert.Equal(1, effects.Count<CompleteOrderCommand>(orderId));

            var effectCountBeforeTerminalReplay = effects.Count<UpdateOrderStatusCommand>(orderId)
                + effects.Count<CancelOrderCommand>(orderId)
                + effects.Count<CompleteOrderCommand>(orderId);

            await Task.WhenAll(
                bus.Publish(submitted),
                bus.Publish(payment),
                bus.Publish(Expired(orderId)),
                bus.Publish(shipped),
                bus.Publish(delivered));

            await Task.Delay(500);
            await AssertStateAsync(scope, orderId, "Completed");
            Assert.Equal(
                effectCountBeforeTerminalReplay,
                effects.Count<UpdateOrderStatusCommand>(orderId)
                + effects.Count<CancelOrderCommand>(orderId)
                + effects.Count<CompleteOrderCommand>(orderId));
        }
        finally
        {
            await OrderSagaSystemHost.StopAsync(host);
        }
    }

    [OrderSagaSystemFact]
    [Trait("Category", "System")]
    public async Task Concurrent_payment_timeout_deliveries_commit_exactly_one_outcome()
    {
        await using var scope = await fixture.CreateScopeAsync();
        var effects = new EffectProbe();
        var host = OrderSagaSystemHost.Create(scope, effects, TimeSpan.FromSeconds(30));

        try
        {
            await host.StartAsync();
            var bus = host.Services.GetRequiredService<IBus>();

            for (var attempt = 0; attempt < 6; attempt++)
            {
                var orderId = Guid.NewGuid();
                await bus.Publish(Submitted(orderId));
                await WaitForStateAsync(scope, orderId, "Submitted");

                await Task.WhenAll(
                    bus.Publish(Payment(orderId)),
                    bus.Publish(Expired(orderId)));

                var state = await WaitForStateAsync(scope, orderId, "Processing", "Failed");
                await SystemTestWait.UntilAsync(
                    () => Task.FromResult(
                        effects.Count<CommitStockReservationCommand>(orderId)
                        + effects.Count<CancelOrderCommand>(orderId) == 1),
                    DefaultWait);

                Assert.Contains(state, new[] { "Processing", "Failed" });
                Assert.Equal(
                    1,
                    effects.Count<CommitStockReservationCommand>(orderId)
                    + effects.Count<CancelOrderCommand>(orderId));
            }
        }
        finally
        {
            await OrderSagaSystemHost.StopAsync(host);
        }
    }

    [OrderSagaSystemFact]
    [Trait("Category", "System")]
    public async Task PostgreSql_xmin_rejects_a_stale_saga_update()
    {
        await using var scope = await fixture.CreateScopeAsync();
        var orderId = Guid.NewGuid();

        await using (var setup = scope.CreateDbContext())
        {
            setup.OrderStates.Add(NewState(orderId));
            await setup.SaveChangesAsync();
        }

        await using var firstContext = scope.CreateDbContext();
        await using var staleContext = scope.CreateDbContext();
        var first = await firstContext.OrderStates.SingleAsync(x => x.OrderId == orderId);
        var stale = await staleContext.OrderStates.SingleAsync(x => x.OrderId == orderId);

        first.CurrentState = "Processing";
        await firstContext.SaveChangesAsync();

        stale.CurrentState = "Failed";
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => staleContext.SaveChangesAsync());
    }

    [OrderSagaSystemFact]
    [Trait("Category", "System")]
    public async Task Persisted_bus_outbox_dispatches_after_process_restart()
    {
        await using var scope = await fixture.CreateScopeAsync();
        var effects = new EffectProbe();
        var orderId = Guid.NewGuid();
        var stoppedProcess = OrderSagaSystemHost.Create(scope, effects, TimeSpan.FromSeconds(30));

        try
        {
            await using var serviceScope = stoppedProcess.Services.CreateAsyncScope();
            var db = serviceScope.ServiceProvider.GetRequiredService<OrderSagaDbContext>();
            await using var transaction = await db.Database.BeginTransactionAsync();
            db.OrderStates.Add(NewState(orderId));
            await serviceScope.ServiceProvider.GetRequiredService<IPublishEndpoint>()
                .Publish(Outgoing(orderId));
            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            Assert.Equal(1, await db.Set<OutboxMessage>().CountAsync());
        }
        finally
        {
            stoppedProcess.Dispose();
        }

        var restartedProcess = OrderSagaSystemHost.Create(scope, effects, TimeSpan.FromSeconds(30));
        try
        {
            await restartedProcess.StartAsync();
            await effects.WaitForCountAsync<UpdateOrderStatusCommand>(
                orderId,
                1,
                x => x.Status == "Paid");

            await SystemTestWait.UntilAsync(async () =>
            {
                await using var db = scope.CreateDbContext();
                return await db.Set<OutboxMessage>().CountAsync() == 0;
            }, DefaultWait);

            await AssertStateAsync(scope, orderId, "Submitted");
        }
        finally
        {
            await OrderSagaSystemHost.StopAsync(restartedProcess);
        }
    }

    private static async Task<string> WaitForStateAsync(
        OrderSagaSystemScope scope,
        Guid orderId,
        params string[] expectedStates)
    {
        string? state = null;
        await SystemTestWait.UntilAsync(async () =>
        {
            await using var db = scope.CreateDbContext();
            state = await db.OrderStates
                .Where(x => x.OrderId == orderId)
                .Select(x => x.CurrentState)
                .SingleOrDefaultAsync();
            return state is not null && expectedStates.Contains(state);
        }, DefaultWait);
        return state!;
    }

    private static async Task<string> WaitForStateAsync(
        OrderSagaSystemScope scope,
        Guid orderId,
        EffectProbe effects,
        params string[] expectedStates)
    {
        try
        {
            return await WaitForStateAsync(scope, orderId, expectedStates);
        }
        catch (Exception exception)
        {
            var queueSummary = await scope.QueueSummaryAsync();
            await using var db = scope.CreateDbContext();
            var persistedStates = await db.OrderStates
                .Select(x => $"{x.OrderId}:{x.CurrentState}")
                .ToListAsync();
            var inboxCount = await db.Set<InboxState>().CountAsync();
            var outboxCount = await db.Set<OutboxMessage>().CountAsync();
            throw new InvalidOperationException(
                $"Saga faults:{Environment.NewLine}{effects.FaultSummary}{Environment.NewLine}"
                + $"Persisted states: {string.Join(", ", persistedStates)}{Environment.NewLine}"
                + $"Inbox/outbox rows: {inboxCount}/{outboxCount}{Environment.NewLine}"
                + $"RabbitMQ queues:{Environment.NewLine}{queueSummary}",
                exception);
        }
    }

    private static async Task AssertStateAsync(
        OrderSagaSystemScope scope,
        Guid orderId,
        string expectedState)
    {
        await using var db = scope.CreateDbContext();
        Assert.Equal(
            expectedState,
            await db.OrderStates
                .Where(x => x.OrderId == orderId)
                .Select(x => x.CurrentState)
                .SingleAsync());
    }

    private static async Task<int> QuartzTriggerCountAsync(OrderSagaSystemScope scope)
    {
        await using var connection = new NpgsqlConnection(scope.DatabaseConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM qrtz_triggers";
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private static OrderSubmittedEvent Submitted(Guid orderId) => new()
    {
        EventId = Guid.NewGuid(),
        OrderId = orderId,
        CustomerId = Guid.NewGuid(),
        BasketClientId = Guid.NewGuid(),
        PaymentId = PaymentId(orderId),
        ReservationId = ReservationId(orderId),
        CustomerName = "System Test Customer",
        CustomerEmail = "system-test@example.invalid",
        TotalAmount = 125.50m,
        DestinationAddress = "System test address"
    };

    private static PaymentCompletedEvent Payment(Guid orderId) => new()
    {
        EventId = Guid.NewGuid(),
        OrderId = orderId,
        PaymentId = PaymentId(orderId),
        ProviderTransactionId = $"provider-{orderId:N}",
        PSPTransactionId = $"psp-{orderId:N}",
        Amount = 125.50m,
        Currency = "USD"
    };

    private static OrderExpiredEvent Expired(Guid orderId) => new()
    {
        EventId = Guid.NewGuid(),
        OrderId = orderId,
        ExpiredAt = DateTime.UtcNow
    };

    private static ShippingScheduledEvent ShippingScheduled(Guid orderId) => new()
    {
        EventId = Guid.NewGuid(),
        OrderId = orderId,
        ShipmentId = ShipmentId(orderId),
        Carrier = "System Carrier",
        TrackingNumber = $"TRACK-{orderId:N}",
        DestinationAddress = "System test address"
    };

    private static StockReservationCommittedEvent StockCommitted(Guid orderId) => new()
    {
        EventId = Guid.NewGuid(),
        OrderId = orderId,
        ReservationId = ReservationId(orderId),
        CommittedAt = DateTime.UtcNow
    };

    private static OrderReadyForPickupEvent ReadyForPickup(Guid orderId) => new()
    {
        EventId = Guid.NewGuid(),
        OrderId = orderId,
        ReadyAt = DateTime.UtcNow,
        OperatorName = "System Test"
    };

    private static OrderShippedEvent Shipped(Guid orderId) => new()
    {
        EventId = Guid.NewGuid(),
        OrderId = orderId,
        Carrier = "System Carrier",
        TrackingNumber = $"TRACK-{orderId:N}",
        ShippedAt = DateTime.UtcNow
    };

    private static OrderDeliveredEvent Delivered(Guid orderId) => new()
    {
        EventId = Guid.NewGuid(),
        OrderId = orderId,
        TrackingNumber = $"TRACK-{orderId:N}",
        DeliveredAt = DateTime.UtcNow
    };

    private static OrderState NewState(Guid orderId) => new()
    {
        CorrelationId = orderId,
        OrderId = orderId,
        CurrentState = "Submitted",
        CustomerId = Guid.NewGuid(),
        BasketClientId = Guid.NewGuid(),
        PaymentId = PaymentId(orderId),
        ReservationId = ReservationId(orderId),
        CustomerName = "System Test Customer",
        CustomerEmail = "system-test@example.invalid",
        DestinationAddress = "System test address",
        ProviderTransactionId = string.Empty,
        TotalAmount = 10m
    };

    private static UpdateOrderStatusCommand Outgoing(Guid orderId) => new()
    {
        EventId = Guid.NewGuid(),
        OrderId = orderId,
        Status = "Paid"
    };

    private static Guid PaymentId(Guid orderId) => StableId(orderId, 1);

    private static Guid ReservationId(Guid orderId) => StableId(orderId, 2);

    private static Guid ShipmentId(Guid orderId) => StableId(orderId, 3);

    private static Guid StableId(Guid orderId, byte discriminator)
    {
        var bytes = orderId.ToByteArray();
        bytes[^1] ^= discriminator;
        return new Guid(bytes);
    }
}
