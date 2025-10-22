using Domain.Common.Events;
using Domain.Common.States;
using MassTransit;

namespace Application.Saga;

public class StockReservationStateMachine : MassTransitStateMachine<StockReservationState>
{
    public State Reserved { get; private set; } = null!;
    public State Committed { get; private set; } = null!;
    public State Cancelled { get; private set; } = null!;
    public State Expired { get; private set; } = null!;

    public Event<StockReservationCreatedEvent> ReservationCreated { get; private set; } = null!;
    public Event<StockReservationCommitedEvent> ReservationCommitted { get; private set; } = null!;
    public Event<StockReservationCancelledEvent> ReservationCancelled { get; private set; } = null!;
    public Schedule<StockReservationState, StockReservationExpiredEvent> ExpirationTimer { get; private set; } = null!;

    public StockReservationStateMachine()
    {
        InstanceState(x => x.CurrentState);

        Event(() => ReservationCreated, x => x.CorrelateById(m => m.Message.ReservationId));
        Event(() => ReservationCommitted, x => x.CorrelateById(m => m.Message.ReservationId));
        Event(() => ReservationCancelled, x => x.CorrelateById(m => m.Message.ReservationId));

        Schedule(() => ExpirationTimer, x => x.ExpirationToken, s =>
        {
            s.Delay = TimeSpan.FromMinutes(5);
            s.Received = e => e.CorrelateById(ctx => ctx.Message.ReservationId);
        });

        Initially(
            When(ReservationCreated)
                .Then(ctx =>
                {
                    ctx.Saga.OrderId = ctx.Message.OrderId;
                    ctx.Saga.CreatedAt = DateTime.UtcNow;
                    Console.WriteLine($"[Saga] Reservation created for Order {ctx.Message.OrderId}");
                })
                .Schedule(ExpirationTimer, ctx => new StockReservationExpiredEvent
                {
                    ReservationId = ctx.Message.ReservationId,
                    OrderId = ctx.Message.OrderId
                })
                .TransitionTo(Reserved)
        );

        During(Reserved,
            When(ReservationCommitted)
                .Then(ctx =>
                {
                    ctx.Saga.IsCommitted = true;
                    Console.WriteLine($"[Saga] Reservation committed for Order {ctx.Saga.OrderId}");
                })
                .Unschedule(ExpirationTimer)
                .TransitionTo(Committed),

            When(ReservationCancelled)
                .Then(ctx =>
                {
                    ctx.Saga.IsCancelled = true;
                    Console.WriteLine($"[Saga] Reservation cancelled for Order {ctx.Saga.OrderId}");
                })
                .Unschedule(ExpirationTimer)
                .TransitionTo(Cancelled),

            When(ExpirationTimer.Received)
                .IfElse(
                    ctx => ctx.Saga.IsCommitted || ctx.Saga.IsCancelled,
                    thenBinder => thenBinder.Then(_ => Console.WriteLine("[Saga] Expiration ignored (already closed).")),
                    elseBinder => elseBinder
                        .ThenAsync(async ctx =>
                        {
                            Console.WriteLine($"[Saga] Reservation expired for Order {ctx.Saga.OrderId}");
                            await ctx.Publish(new StockReservationExpiredEvent
                            {
                                ReservationId = ctx.Saga.CorrelationId,
                                OrderId = ctx.Saga.OrderId
                            });
                        })
                        .TransitionTo(Expired))
        );

        SetCompletedWhenFinalized();
    }
}