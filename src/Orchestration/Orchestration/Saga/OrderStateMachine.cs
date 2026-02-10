using Domain.Common.Commands.Operations;
using Domain.Common.Commands.Basket;
using Domain.Common.Commands.Orders;
using Domain.Common.Commands.Payments;
using Domain.Common.Commands.Shipping;
using Domain.Common.Commands.Stock;
using Domain.Common.Events.Orders;
using Domain.Common.Events.Payments;
using Domain.Common.Events.Stock;
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
    public Event<StockReservationCommittedEvent> StockReservationCommitted { get; private set; } = null!;
    public Event<StockReservationCommitFailedEvent> StockReservationCommitFailed { get; private set; } = null!;
    public Event<OrderReadyForPickupEvent> OrderReadyForPickup { get; private set; } = null!;

    public OrderStateMachine()
    {
        InstanceState(x => x.CurrentState);

        Event(() => OrderSubmitted, x => x.CorrelateById(m => m.Message.OrderId).SelectId(m => m.Message.OrderId));
        Event(() => OrderCompleted, x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => PaymentCompleted, cfg => cfg.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => PaymentFailed, cfg => cfg.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => StockReservationCommitted, cfg => cfg.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => StockReservationCommitFailed, cfg => cfg.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => OrderReadyForPickup, cfg => cfg.CorrelateById(ctx => ctx.Message.OrderId));

        // === INITIAL ===
        Initially(
            When(OrderSubmitted)
                .Then(ctx =>
                {
                    ctx.Saga.OrderId       = ctx.Message.OrderId;
                    ctx.Saga.CustomerName  = ctx.Message.CustomerName;
                    ctx.Saga.CustomerEmail = ctx.Message.CustomerEmail;
                    ctx.Saga.CustomerId    = ctx.Message.CustomerId;
                    ctx.Saga.TotalAmount   = ctx.Message.TotalAmount;
                    ctx.Saga.PaymentId     = ctx.Message.PaymentId;
                    ctx.Saga.ReservationId = ctx.Message.ReservationId;
                    Console.WriteLine($"[Saga] Order submitted: {ctx.Saga.OrderId}");
                })
                .TransitionTo(Submitted)
        );

        // === SUBMITTED ===
        During(Submitted,
            When(PaymentCompleted)
                .Then(ctx =>
                {
                    ctx.Saga.ProviderTransactionId = ctx.Message.ProviderTransactionId;
                    ctx.Saga.PaymentId = ctx.Message.PaymentId;
                    Console.WriteLine($"[Saga] Payment completed for order {ctx.Saga.CorrelationId}");
                })
                .Publish(ctx => new CommitStockReservationCommand
                {
                    OrderId = ctx.Saga.CorrelationId,
                    ReservationId = ctx.Saga.ReservationId
                })
                .Publish(ctx => new EmptyBasketCommand
                {
                    OrderId = ctx.Saga.CorrelationId,
                    ClientId = ctx.Saga.CustomerId
                })
                .Publish(ctx => new ScheduleShippingCommand
                {
                    OrderId = ctx.Saga.CorrelationId,
                    CustomerEmail = ctx.Saga.CustomerEmail,
                    DestinationAddress = ctx.Saga.DestinationAddress
                }),

            When(StockReservationCommitted)
                .Then(ctx =>
                {
                    Console.WriteLine($"[Saga] Stock committed for order {ctx.Saga.CorrelationId}");
                })
                .Publish(ctx => new PreparePackageCommand
                {
                    OrderId = ctx.Saga.CorrelationId,
                    ReservationId = ctx.Message.ReservationId
                }),

            When(OrderReadyForPickup)
                .Then(ctx => Console.WriteLine("[Saga] Order {0} ready for pickup.", ctx.Saga.CorrelationId))
                .Publish(ctx => new ConfirmShippingCommand
                {
                    OrderId = ctx.Saga.CorrelationId,
                    ShippingId = ctx.Saga.CorrelationId,
                    ReadyAt = ctx.Message.ReadyAt
                }),

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
                .Finalize(),

            When(StockReservationCommitFailed)
                .Then(ctx =>
                {
                    Console.WriteLine($"[Saga] Stock commit failed for order {ctx.Saga.CorrelationId}");
                })
                .Publish(ctx => new RefundPaymentCommand
                {
                    OrderId = ctx.Saga.CorrelationId,
                    PaymentId = ctx.Saga.PaymentId,
                    ProviderTransactionId = ctx.Saga.ProviderTransactionId,
                    Amount = ctx.Saga.TotalAmount,
                    Reason = ctx.Message.Reason
                })
                .Publish(ctx => new CancelShippingCommand
                {
                    OrderId = ctx.Saga.CorrelationId,
                    ShippingId = ctx.Saga.CorrelationId
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

