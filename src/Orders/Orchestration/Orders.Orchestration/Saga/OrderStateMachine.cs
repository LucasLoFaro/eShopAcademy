using Domain.Common.Events;
using Domain.Common.States;
using Domain.Common.Commands;
using MassTransit;
using Orchestration.Middlewares;


namespace Application.Saga;

public class OrderStateMachine : MassTransitStateMachine<OrderState>
{
    // Saga States
    public State Submitted { get; private set; } = null!;
    public State Completed { get; private set; } = null!;
    public State Failed { get; private set; } = null!;

    // Domain Events
    public Event<OrderSubmittedEvent> OrderSubmitted { get; private set; } = null!;
    public Event<OrderCompletedEvent> OrderCompleted { get; private set; } = null!;
    public Event<PaymentCompletedEvent> PaymentCompleted { get; private set; } = null!;
    public Event<PaymentFailedEvent> PaymentFailed { get; private set; } = null!;



    public OrderStateMachine()
    {
        InstanceState(x => x.CurrentState);

        // Mailing on status change disabled for now. The notification service is subscribed to the events directly.
        //ConnectStateObserver(new OrderStateObserver());

        Event(() => OrderSubmitted, x => x.CorrelateById(m => m.Message.OrderId).SelectId(m => m.Message.OrderId));
        Event(() => OrderCompleted, x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => PaymentCompleted, cfg => cfg.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => PaymentFailed, cfg => cfg.CorrelateById(ctx => ctx.Message.OrderId));

        // === INITIAL ===
        Initially(
            When(OrderSubmitted)
                .Then(ctx =>
                {
                    ctx.Saga.OrderId       = ctx.Message.OrderId;
                    ctx.Saga.CustomerEmail = ctx.Message.CustomerEmail;
                    Console.WriteLine($"[Saga] Order submitted: {ctx.Saga.OrderId}");
                })
                .TransitionTo(Submitted)
        );

        // === SUBMITTED ===
        During(Submitted,
            When(PaymentCompleted)
                .Then(ctx =>
                {
                    Console.WriteLine($"[Saga] Payment completed for order {ctx.Saga.CorrelationId}");
                    // ToDo: Update status on DB. Should I?
                })
                .Publish(ctx => new OrderCompletedEvent
                {
                    OrderId = ctx.Saga.CorrelationId,
                    CustomerEmail = ctx.Saga.CustomerEmail
                })
                .TransitionTo(Completed)
                .Finalize(),

            When(PaymentFailed)
                .Then(ctx =>
                {
                    Console.WriteLine($"[Saga] Payment failed for order {ctx.Saga.CorrelationId}");
                })
                .TransitionTo(Failed)
                .Finalize()
        );

        SetCompletedWhenFinalized();
    }
}