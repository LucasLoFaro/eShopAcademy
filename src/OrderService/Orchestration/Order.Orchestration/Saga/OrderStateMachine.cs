using Core.Domain.States;
using Core.Domain.Events;
using Domain.Commands;
using MassTransit;


namespace Application.Saga;

public class OrderStateMachine : MassTransitStateMachine<OrderState>
{
    // Saga States
    public State Submitted { get; private set; } = null!;
    public State StockReserving { get; private set; } = null!;
    public State Shipping { get; private set; } = null!;
    public State Completed { get; private set; } = null!;
    public State Failed { get; private set; } = null!;

    // Domain Events
    public Event<OrderSubmittedEvent> OrderSubmitted { get; private set; } = null!;
    public Event<PaymentCompletedEvent> PaymentCompleted { get; private set; } = null!;
    public Event<PaymentFailedEvent> PaymentFailed { get; private set; } = null!;
    public Event<StockReservationCommitedEvent> StockCommitted { get; private set; } = null!;
    public Event<StockReservationCancelledEvent> StockCancelled { get; private set; } = null!;
    public Event<StockReservationExpiredEvent> StockExpired { get; private set; } = null!;
    public Event<ShippingStartedEvent> ShippingStarted { get; private set; } = null!;
    public Event<ShippingCompletedEvent> ShippingCompleted { get; private set; } = null!;


    // Built-in Scheduler (native MT 8 API)
    public Schedule<OrderState, StockReservationExpiredEvent> ExpirationTimer { get; private set; } = null!;

    public OrderStateMachine()
    {
        InstanceState(x => x.CurrentState);

        // Correlation by OrderId
        Event(() => OrderSubmitted,  x => x.CorrelateById(m => m.Message.OrderId));
        Event(() => PaymentCompleted, x => x.CorrelateById(m => m.Message.OrderId));
        Event(() => PaymentFailed,    x => x.CorrelateById(m => m.Message.OrderId));
        Event(() => StockCommitted,   x => x.CorrelateById(m => m.Message.OrderId));
        Event(() => StockCancelled,   x => x.CorrelateById(m => m.Message.OrderId));
        Event(() => StockExpired,     x => x.CorrelateById(m => m.Message.OrderId));
        Event(() => ShippingStarted,  x => x.CorrelateById(m => m.Message.OrderId));
        Event(() => ShippingCompleted,x => x.CorrelateById(m => m.Message.OrderId));

        Schedule(() => ExpirationTimer, x => x.ExpirationToken, s =>
        {
            s.Delay = TimeSpan.FromMinutes(15);
            s.Received = e => e.CorrelateById(ctx => ctx.Message.OrderId);
        });

        // === INITIAL ===
        Initially(
            When(OrderSubmitted)
                .Then(ctx =>
                {
                    ctx.Saga.OrderId       = ctx.Message.OrderId;
                    ctx.Saga.PaymentId     = ctx.Message.PaymentId;
                    ctx.Saga.ReservationId = ctx.Message.ReservationId;
                    ctx.Saga.CreatedAt     = DateTime.UtcNow;
                    Console.WriteLine($"[Saga] Order submitted: {ctx.Saga.OrderId}");
                })
                .Schedule(ExpirationTimer, ctx => new StockReservationExpiredEvent
                {
                    OrderId       = ctx.Message.OrderId,
                    ReservationId = ctx.Message.ReservationId
                })
                .TransitionTo(Submitted)
        );

        // === SUBMITTED ===
        During(Submitted,
            When(PaymentCompleted)
                .Unschedule(ExpirationTimer)
                .Then(ctx => Console.WriteLine($"[Saga] Payment completed: {ctx.Saga.OrderId}"))
                .Publish(ctx => new StockReservationCommitedEvent
                {
                    OrderId       = ctx.Saga.OrderId,
                    ReservationId = ctx.Saga.ReservationId
                })
                .TransitionTo(StockReserving),

            When(PaymentFailed)
                .Unschedule(ExpirationTimer)
                .Then(ctx => Console.WriteLine($"[Saga] Payment failed: {ctx.Saga.OrderId}"))
                .Publish(ctx => new StockReservationCancelledEvent
                {
                    OrderId       = ctx.Saga.OrderId,
                    ReservationId = ctx.Saga.ReservationId,
                    Reason        = ctx.Message.Reason
                })
                .TransitionTo(Failed),

            When(StockExpired)
                .Then(ctx => Console.WriteLine($"[Saga] Order expired: {ctx.Saga.OrderId}"))
                .Publish(ctx => new StockReservationCancelledEvent
                {
                    OrderId       = ctx.Saga.OrderId,
                    ReservationId = ctx.Saga.ReservationId,
                    Reason        = "Payment timeout"
                })
                .TransitionTo(Failed)
        );

        // === STOCK RESERVING ===
        During(StockReserving,
            When(StockCommitted)
                .Then(ctx => Console.WriteLine($"[Saga] Stock committed for order {ctx.Saga.OrderId}"))
                .Publish(ctx => new StartShippingCommand
                {
                    OrderId            = ctx.Saga.OrderId,
                    PaymentId          = ctx.Saga.PaymentId,
                    StockReservationId = ctx.Saga.ReservationId
                })
                .TransitionTo(Shipping),

            When(StockCancelled)
                .Then(ctx => Console.WriteLine($"[Saga] Stock reservation cancelled: {ctx.Saga.OrderId}"))
                .TransitionTo(Failed)
        );

        // === SHIPPING ===
        During(Shipping,
            When(ShippingStarted)
                .Then(ctx => Console.WriteLine($"[Saga] Shipping started: {ctx.Saga.OrderId}")),

            When(ShippingCompleted)
                .Then(ctx => Console.WriteLine($"[Saga] Order completed: {ctx.Saga.OrderId}"))
                .TransitionTo(Completed)
        );

        SetCompletedWhenFinalized();
    }
}