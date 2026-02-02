using Common.Domain.Commands.Orders;
using Common.Domain.Events.Orders;
using Common.Domain.Events.Payments;
using Domain.Common.States;
using MassTransit;


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
                    ctx.Saga.CustomerName  = ctx.Message.CustomerName;
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
                })
                .Publish(ctx => new OrderCompletedEvent
                {
                    OrderId = ctx.Saga.CorrelationId,
                    CustomerName = ctx.Saga.CustomerName,
                    CustomerEmail = ctx.Saga.CustomerEmail
                })
                .TransitionTo(Completed)
                .Finalize(),

            When(PaymentFailed)
                .Then(ctx =>
                {
                    Console.WriteLine($"[Saga] Payment failed for order {ctx.Saga.CorrelationId}");
                })
                .Publish(ctx => new CancelOrderCommand
                {
                    OrderId = ctx.Saga.CorrelationId,
                    CustomerName = ctx.Saga.CustomerName,
                    CustomerEmail = ctx.Saga.CustomerEmail,
                    Reason = ctx.Message.Reason
                })
                .TransitionTo(Failed)
                .Finalize()
        );

        SetCompletedWhenFinalized();
    }
}

