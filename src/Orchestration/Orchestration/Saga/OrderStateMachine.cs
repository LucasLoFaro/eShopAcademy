using System.Security.Cryptography;
using System.Text;
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
using Microsoft.Extensions.Options;

namespace Application.Saga;

public sealed class OrderStateMachine : MassTransitStateMachine<OrderState>
{
    private readonly ILogger<OrderStateMachine> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _paymentTimeout;

    public State Submitted { get; private set; } = null!;
    public State Processing { get; private set; } = null!;
    public State ShippingScheduled { get; private set; } = null!;
    public State StockCommitted { get; private set; } = null!;
    public State Fulfilling { get; private set; } = null!;
    public State ReadyForPickup { get; private set; } = null!;
    public State Shipped { get; private set; } = null!;
    public State Completed { get; private set; } = null!;
    public State Failed { get; private set; } = null!;

    public Event<OrderSubmittedEvent> OrderSubmitted { get; private set; } = null!;
    public Event<OrderCompletedEvent> OrderCompleted { get; private set; } = null!;
    public Event<PaymentCompletedEvent> PaymentCompleted { get; private set; } = null!;
    public Event<PaymentFailedEvent> PaymentFailed { get; private set; } = null!;
    public Event<StockReservationCommittedEvent> StockReservationCommitted { get; private set; } = null!;
    public Event<StockReservationCommitFailedEvent> StockReservationCommitFailed { get; private set; } = null!;
    public Event<OrderReadyForPickupEvent> OrderReadyForPickup { get; private set; } = null!;
    public Event<PackageIssueReportedEvent> PackageIssueReported { get; private set; } = null!;
    public Event<ShippingFailedEvent> ShippingFailed { get; private set; } = null!;
    public Event<ShippingScheduledEvent> ShippingScheduledEvent { get; private set; } = null!;
    public Event<OrderShippedEvent> OrderShipped { get; private set; } = null!;
    public Event<OrderDeliveredEvent> OrderDelivered { get; private set; } = null!;

    public Schedule<OrderState, OrderExpiredEvent> PaymentTimeout { get; private set; } = null!;

    public OrderStateMachine(
        IOptions<OrderSagaOptions> options,
        TimeProvider timeProvider,
        ILogger<OrderStateMachine> logger)
    {
        _paymentTimeout = options.Value.PaymentTimeout;
        _timeProvider = timeProvider;
        _logger = logger;

        InstanceState(x => x.CurrentState);

        Event(() => OrderSubmitted, x => x.CorrelateById(m => m.Message.OrderId).SelectId(m => m.Message.OrderId));
        Event(() => OrderCompleted, x => x.CorrelateById(ctx => ctx.Message.OrderId).OnMissingInstance(m => m.Discard()));
        Event(() => PaymentCompleted, x => x.CorrelateById(ctx => ctx.Message.OrderId).OnMissingInstance(m => m.Discard()));
        Event(() => PaymentFailed, x => x.CorrelateById(ctx => ctx.Message.OrderId).OnMissingInstance(m => m.Discard()));
        Event(() => StockReservationCommitted, x => x.CorrelateById(ctx => ctx.Message.OrderId).OnMissingInstance(m => m.Discard()));
        Event(() => StockReservationCommitFailed, x => x.CorrelateById(ctx => ctx.Message.OrderId).OnMissingInstance(m => m.Discard()));
        Event(() => OrderReadyForPickup, x => x.CorrelateById(ctx => ctx.Message.OrderId).OnMissingInstance(m => m.Discard()));
        Event(() => PackageIssueReported, x => x.CorrelateById(ctx => ctx.Message.OrderId).OnMissingInstance(m => m.Discard()));
        Event(() => ShippingFailed, x => x.CorrelateById(ctx => ctx.Message.OrderId).OnMissingInstance(m => m.Discard()));
        Event(() => ShippingScheduledEvent, x => x.CorrelateById(ctx => ctx.Message.OrderId).OnMissingInstance(m => m.Discard()));
        Event(() => OrderShipped, x => x.CorrelateById(ctx => ctx.Message.OrderId).OnMissingInstance(m => m.Discard()));
        Event(() => OrderDelivered, x => x.CorrelateById(ctx => ctx.Message.OrderId).OnMissingInstance(m => m.Discard()));

        Schedule(() => PaymentTimeout, saga => saga.PaymentTimeoutTokenId, x =>
        {
            x.Delay = _paymentTimeout;
            x.Received = r => r.CorrelateById(ctx => ctx.Message.OrderId).OnMissingInstance(m => m.Discard());
        });

        Initially(
            When(OrderSubmitted)
                .Then(ctx =>
                {
                    ctx.Saga.OrderId = ctx.Message.OrderId;
                    ctx.Saga.CustomerName = ctx.Message.CustomerName;
                    ctx.Saga.CustomerEmail = ctx.Message.CustomerEmail;
                    ctx.Saga.CustomerId = ctx.Message.CustomerId;
                    ctx.Saga.BasketClientId = ctx.Message.BasketClientId;
                    ctx.Saga.TotalAmount = ctx.Message.TotalAmount;
                    ctx.Saga.PaymentId = ctx.Message.PaymentId;
                    ctx.Saga.ReservationId = ctx.Message.ReservationId;
                    ctx.Saga.DestinationAddress = ctx.Message.DestinationAddress;
                    _logger.LogInformation("Order saga submitted for {OrderId}", ctx.Saga.OrderId);
                })
                .Schedule(PaymentTimeout, ctx =>
                {
                    var expiresAt = _timeProvider.GetUtcNow().Add(_paymentTimeout).UtcDateTime;
                    return new OrderExpiredEvent
                    {
                        EventId = EffectId(ctx.Saga.CorrelationId, ctx.Message.EventId, "payment-timeout"),
                        OrderId = ctx.Saga.CorrelationId,
                        ExpiredAt = expiresAt
                    };
                })
                .TransitionTo(Submitted));

        During(Submitted,
            When(PaymentCompleted)
                .Unschedule(PaymentTimeout)
                .Then(ctx =>
                {
                    ctx.Saga.ProviderTransactionId = ctx.Message.ProviderTransactionId;
                    ctx.Saga.PaymentId = ctx.Message.PaymentId;
                    _logger.LogInformation("Payment completed for order saga {OrderId}", ctx.Saga.CorrelationId);
                })
                .Send(new Uri("queue:commit-stock-reservation"), ctx => new CommitStockReservationCommand
                {
                    EventId = EffectId(ctx.Saga.CorrelationId, ctx.Message.EventId, "commit-stock-reservation"),
                    OrderId = ctx.Saga.CorrelationId,
                    ReservationId = ctx.Saga.ReservationId
                })
                .Send(new Uri("queue:empty-basket"), ctx => new EmptyBasketCommand
                {
                    EventId = EffectId(ctx.Saga.CorrelationId, ctx.Message.EventId, "empty-basket"),
                    OrderId = ctx.Saga.CorrelationId,
                    ClientId = ctx.Saga.BasketClientId
                })
                .Send(new Uri("queue:schedule-shipping"), ctx => new ScheduleShippingCommand
                {
                    EventId = EffectId(ctx.Saga.CorrelationId, ctx.Message.EventId, "schedule-shipping"),
                    OrderId = ctx.Saga.CorrelationId,
                    CustomerEmail = ctx.Saga.CustomerEmail,
                    DestinationAddress = ctx.Saga.DestinationAddress
                })
                .Send(new Uri("queue:update-order-status-command"), ctx => new UpdateOrderStatusCommand
                {
                    EventId = EffectId(ctx.Saga.CorrelationId, ctx.Message.EventId, "mark-paid"),
                    OrderId = ctx.Saga.CorrelationId,
                    CustomerName = ctx.Saga.CustomerName,
                    CustomerEmail = ctx.Saga.CustomerEmail,
                    Status = "Paid",
                    PaymentId = ctx.Message.PaymentId,
                    ProviderTransactionId = ctx.Message.ProviderTransactionId,
                    Amount = ctx.Message.Amount,
                    PaymentStatus = "Captured",
                    DestinationAddress = ctx.Saga.DestinationAddress,
                    PaidAt = _timeProvider.GetUtcNow().UtcDateTime
                })
                .TransitionTo(Processing),
            When(PaymentTimeout.Received)
                .Then(ctx => _logger.LogWarning("Payment timed out for order saga {OrderId}", ctx.Saga.CorrelationId))
                .Send(new Uri("queue:release-stock-reservation"), ctx => new ReleaseStockReservationCommand
                {
                    EventId = EffectId(ctx.Saga.CorrelationId, ctx.Message.EventId, "release-stock-reservation"),
                    OrderId = ctx.Saga.CorrelationId,
                    ReservationId = ctx.Saga.ReservationId,
                    Reason = TimeoutReason()
                })
                .Send(new Uri("queue:update-order-status-command"), ctx => CancelledStatus(ctx, "timeout-status"))
                .Send(new Uri("queue:cancel-order-command"), ctx => CancelOrder(ctx, "timeout-cancel", $"Order expired: {TimeoutReason()}"))
                .TransitionTo(Failed),
            When(PaymentFailed)
                .Then(ctx => _logger.LogWarning("Payment failed for order saga {OrderId}", ctx.Saga.CorrelationId))
                .Send(new Uri("queue:release-stock-reservation"), ctx => new ReleaseStockReservationCommand
                {
                    EventId = EffectId(ctx.Saga.CorrelationId, ctx.Message.EventId, "release-stock-reservation"),
                    OrderId = ctx.Saga.CorrelationId,
                    ReservationId = ctx.Saga.ReservationId,
                    Reason = ctx.Message.Reason
                })
                .Send(new Uri("queue:update-order-status-command"), ctx => CancelledStatus(ctx, "payment-failed-status"))
                .Send(new Uri("queue:cancel-order-command"), ctx => CancelOrder(ctx, "payment-failed-cancel", $"Payment failed: {ctx.Message.Reason}"))
                .TransitionTo(Failed));

        During(Processing,
            ShippingScheduledActivity(ShippingScheduled),
            StockCommittedActivity(StockCommitted),
            ShippingFailureActivity(),
            StockCommitFailureActivity(),
            PackageIssueActivity(),
            ShippedActivity());

        During(ShippingScheduled,
            Ignore(ShippingScheduledEvent),
            StockCommittedActivity(Fulfilling),
            ShippingFailureActivity(),
            StockCommitFailureActivity(),
            PackageIssueActivity(),
            ShippedActivity());

        During(StockCommitted,
            Ignore(StockReservationCommitted),
            ShippingScheduledActivity(Fulfilling),
            ShippingFailureActivity(),
            StockCommitFailureActivity(),
            PackageIssueActivity(),
            ShippedActivity());

        During(Fulfilling,
            Ignore(ShippingScheduledEvent),
            Ignore(StockReservationCommitted),
            When(OrderReadyForPickup)
                .Then(ctx => _logger.LogInformation("Order saga {OrderId} is ready for pickup", ctx.Saga.CorrelationId))
                .Send(new Uri("queue:confirm-shipping"), ctx => new ConfirmPickupCommand
                {
                    EventId = EffectId(ctx.Saga.CorrelationId, ctx.Message.EventId, "confirm-shipping"),
                    OrderId = ctx.Saga.CorrelationId,
                    ShippingId = ctx.Saga.ShipmentId,
                    ReadyAt = ctx.Message.ReadyAt
                })
                .Send(new Uri("queue:update-order-status-command"), ctx => new UpdateOrderStatusCommand
                {
                    EventId = EffectId(ctx.Saga.CorrelationId, ctx.Message.EventId, "ready-for-pickup-status"),
                    OrderId = ctx.Saga.CorrelationId,
                    CustomerName = ctx.Saga.CustomerName,
                    CustomerEmail = ctx.Saga.CustomerEmail,
                    Status = "ReadyForPickup",
                    ShippingStatus = "ReadyForPickup",
                    ReadyForPickupAt = ctx.Message.ReadyAt,
                    OperatorName = ctx.Message.OperatorName,
                    PackedAt = ctx.Message.ReadyAt
                })
                .TransitionTo(ReadyForPickup),
            ShippingFailureActivity(),
            PackageIssueActivity(),
            ShippedActivity());

        During(ReadyForPickup,
            Ignore(OrderReadyForPickup),
            Ignore(ShippingScheduledEvent),
            Ignore(StockReservationCommitted),
            ShippingFailureActivity(),
            PackageIssueActivity(),
            ShippedActivity());

        During(Shipped,
            Ignore(PaymentTimeout.Received),
            Ignore(PaymentCompleted),
            Ignore(ShippingScheduledEvent),
            Ignore(StockReservationCommitted),
            Ignore(OrderReadyForPickup),
            Ignore(OrderShipped),
            When(OrderDelivered)
                .Then(ctx => _logger.LogInformation("Order saga {OrderId} was delivered", ctx.Saga.CorrelationId))
                .Publish(ctx => new CompleteOrderCommand
                {
                    EventId = EffectId(ctx.Saga.CorrelationId, ctx.Message.EventId, "complete-order"),
                    OrderId = ctx.Saga.CorrelationId,
                    CustomerName = ctx.Saga.CustomerName,
                    CustomerEmail = ctx.Saga.CustomerEmail
                })
                .Send(new Uri("queue:update-order-status-command"), ctx => new UpdateOrderStatusCommand
                {
                    EventId = EffectId(ctx.Saga.CorrelationId, ctx.Message.EventId, "delivered-status"),
                    OrderId = ctx.Saga.CorrelationId,
                    CustomerName = ctx.Saga.CustomerName,
                    CustomerEmail = ctx.Saga.CustomerEmail,
                    Status = "Delivered",
                    ShippingStatus = "Delivered",
                    TrackingNumber = ctx.Message.TrackingNumber,
                    DeliveredAt = ctx.Message.DeliveredAt
                })
                .TransitionTo(Completed),
            When(ShippingFailed)
                .Then(ctx => _logger.LogWarning("Shipping failed after dispatch for order saga {OrderId}", ctx.Saga.CorrelationId))
                .Send(new Uri("queue:refund-payment"), ctx => Refund(ctx, "post-dispatch-refund", $"Shipping failed after dispatch: {ctx.Message.Reason}"))
                .Send(new Uri("queue:update-order-status-command"), ctx => CancelledStatus(ctx, "post-dispatch-status"))
                .Send(new Uri("queue:cancel-order-command"), ctx => CancelOrder(ctx, "post-dispatch-cancel", $"Shipping failed after dispatch: {ctx.Message.Reason}"))
                .TransitionTo(Failed));

        During(Completed,
            Ignore(OrderSubmitted),
            Ignore(OrderCompleted),
            Ignore(PaymentCompleted),
            Ignore(PaymentFailed),
            Ignore(PaymentTimeout.Received),
            Ignore(StockReservationCommitted),
            Ignore(StockReservationCommitFailed),
            Ignore(OrderReadyForPickup),
            Ignore(PackageIssueReported),
            Ignore(ShippingFailed),
            Ignore(ShippingScheduledEvent),
            Ignore(OrderShipped),
            Ignore(OrderDelivered));

        During(Failed,
            Ignore(OrderSubmitted),
            Ignore(OrderCompleted),
            Ignore(PaymentCompleted),
            Ignore(PaymentFailed),
            Ignore(PaymentTimeout.Received),
            Ignore(StockReservationCommitted),
            Ignore(StockReservationCommitFailed),
            Ignore(OrderReadyForPickup),
            Ignore(PackageIssueReported),
            Ignore(ShippingFailed),
            Ignore(ShippingScheduledEvent),
            Ignore(OrderShipped),
            Ignore(OrderDelivered));

        DuringAny(
            Ignore(OrderCompleted),
            Ignore(PaymentTimeout.Received, x => x.Saga.CurrentState != Submitted.Name),
            Ignore(PaymentCompleted, x => x.Saga.CurrentState != Submitted.Name));
    }

    private EventActivityBinder<OrderState, ShippingScheduledEvent> ShippingScheduledActivity(State nextState) =>
        When(ShippingScheduledEvent)
            .Then(ctx =>
            {
                ctx.Saga.ShipmentId = ctx.Message.ShipmentId;
                _logger.LogInformation(
                    "Shipping {ShipmentId} was scheduled for order saga {OrderId}",
                    ctx.Message.ShipmentId,
                    ctx.Saga.CorrelationId);
            })
            .Send(new Uri("queue:update-order-status-command"), ctx => new UpdateOrderStatusCommand
            {
                EventId = EffectId(ctx.Saga.CorrelationId, ctx.Message.EventId, "shipping-scheduled-status"),
                OrderId = ctx.Saga.CorrelationId,
                CustomerName = ctx.Saga.CustomerName,
                CustomerEmail = ctx.Saga.CustomerEmail,
                Status = "Paid",
                ShippingStatus = "Scheduled",
                TrackingNumber = ctx.Message.TrackingNumber,
                Carrier = ctx.Message.Carrier,
                DestinationAddress = ctx.Message.DestinationAddress
            })
            .TransitionTo(nextState);

    private EventActivityBinder<OrderState, StockReservationCommittedEvent> StockCommittedActivity(State nextState) =>
        When(StockReservationCommitted)
            .Then(ctx => _logger.LogInformation("Stock was committed for order saga {OrderId}", ctx.Saga.CorrelationId))
            .Send(new Uri("queue:prepare-package"), ctx => new PreparePackageCommand
            {
                EventId = EffectId(ctx.Saga.CorrelationId, ctx.Message.EventId, "prepare-package"),
                OrderId = ctx.Saga.CorrelationId,
                ReservationId = ctx.Message.ReservationId,
                CustomerName = ctx.Saga.CustomerName,
                CustomerEmail = ctx.Saga.CustomerEmail
            })
            .Send(new Uri("queue:update-order-status-command"), ctx => new UpdateOrderStatusCommand
            {
                EventId = EffectId(ctx.Saga.CorrelationId, ctx.Message.EventId, "stock-committed-status"),
                OrderId = ctx.Saga.CorrelationId,
                CustomerName = ctx.Saga.CustomerName,
                CustomerEmail = ctx.Saga.CustomerEmail,
                Status = "Processing",
                ShippingStatus = "Confirmed",
                ReservationId = ctx.Message.ReservationId,
                StockCommittedAt = ctx.Message.CommittedAt
            })
            .TransitionTo(nextState);

    private EventActivityBinder<OrderState, ShippingFailedEvent> ShippingFailureActivity() =>
        When(ShippingFailed)
            .Then(ctx => _logger.LogWarning("Shipping failed for order saga {OrderId}", ctx.Saga.CorrelationId))
            .Send(new Uri("queue:refund-payment"), ctx => Refund(ctx, "shipping-failed-refund", $"Shipping failed: {ctx.Message.Reason}"))
            .Send(new Uri("queue:release-stock-reservation"), ctx => new ReleaseStockReservationCommand
            {
                EventId = EffectId(ctx.Saga.CorrelationId, ctx.Message.EventId, "shipping-failed-release"),
                OrderId = ctx.Saga.CorrelationId,
                ReservationId = ctx.Saga.ReservationId,
                Reason = $"Shipping failed: {ctx.Message.Reason}"
            })
            .Send(new Uri("queue:update-order-status-command"), ctx => CancelledStatus(ctx, "shipping-failed-status"))
            .Send(new Uri("queue:cancel-order-command"), ctx => CancelOrder(ctx, "shipping-failed-cancel", $"Shipping failed: {ctx.Message.Reason}"))
            .TransitionTo(Failed);

    private EventActivityBinder<OrderState, StockReservationCommitFailedEvent> StockCommitFailureActivity() =>
        When(StockReservationCommitFailed)
            .Then(ctx => _logger.LogWarning("Stock commit failed for order saga {OrderId}", ctx.Saga.CorrelationId))
            .Send(new Uri("queue:refund-payment"), ctx => Refund(ctx, "stock-failed-refund", ctx.Message.Reason))
            .If(ctx => ctx.Saga.ShipmentId != Guid.Empty, activity => activity
                .Send(new Uri("queue:cancel-shipping"), ctx => CancelShipping(ctx, "stock-failed-cancel-shipping")))
            .Send(new Uri("queue:update-order-status-command"), ctx => CancelledStatus(ctx, "stock-failed-status"))
            .Send(new Uri("queue:cancel-order-command"), ctx => CancelOrder(ctx, "stock-failed-cancel", $"Stock reservation failed: {ctx.Message.Reason}"))
            .TransitionTo(Failed);

    private EventActivityBinder<OrderState, PackageIssueReportedEvent> PackageIssueActivity() =>
        When(PackageIssueReported)
            .Then(ctx =>
            {
                ctx.Saga.IssueType = ctx.Message.IssueType;
                ctx.Saga.IssueDetails = ctx.Message.Details;
                ctx.Saga.IssueReportedAt = ctx.Message.ReportedAt;
                _logger.LogWarning("Package issue {IssueType} for order saga {OrderId}", ctx.Message.IssueType, ctx.Saga.CorrelationId);
            })
            .Send(new Uri("queue:refund-payment"), ctx => Refund(ctx, "package-issue-refund", $"Package issue: {ctx.Message.IssueType}"))
            .Send(new Uri("queue:release-stock-reservation"), ctx => new ReleaseStockReservationCommand
            {
                EventId = EffectId(ctx.Saga.CorrelationId, ctx.Message.EventId, "package-issue-release"),
                OrderId = ctx.Saga.CorrelationId,
                ReservationId = ctx.Saga.ReservationId,
                Reason = $"Package issue: {ctx.Message.IssueType}"
            })
            .If(ctx => ctx.Saga.ShipmentId != Guid.Empty, activity => activity
                .Send(new Uri("queue:cancel-shipping"), ctx => CancelShipping(ctx, "package-issue-cancel-shipping")))
            .Send(new Uri("queue:update-order-status-command"), ctx => CancelledStatus(ctx, "package-issue-status"))
            .Send(new Uri("queue:cancel-order-command"), ctx => CancelOrder(ctx, "package-issue-cancel", $"Package issue: {ctx.Message.IssueType} — {ctx.Message.Details}"))
            .TransitionTo(Failed);

    private EventActivityBinder<OrderState, OrderShippedEvent> ShippedActivity() =>
        When(OrderShipped)
            .Then(ctx => _logger.LogInformation("Order saga {OrderId} was shipped", ctx.Saga.CorrelationId))
            .Send(new Uri("queue:update-order-status-command"), ctx => new UpdateOrderStatusCommand
            {
                EventId = EffectId(ctx.Saga.CorrelationId, ctx.Message.EventId, "shipped-status"),
                OrderId = ctx.Saga.CorrelationId,
                CustomerName = ctx.Saga.CustomerName,
                CustomerEmail = ctx.Saga.CustomerEmail,
                Status = "Shipped",
                ShippingStatus = "Shipped",
                TrackingNumber = ctx.Message.TrackingNumber,
                Carrier = ctx.Message.Carrier,
                ShippedAt = ctx.Message.ShippedAt
            })
            .TransitionTo(Shipped);

    private RefundPaymentCommand Refund<T>(BehaviorContext<OrderState, T> ctx, string effect, string reason)
        where T : class => new()
        {
            EventId = EffectId(ctx.Saga.CorrelationId, ctx.Message is Domain.Common.BaseMessage message ? message.EventId : Guid.Empty, effect),
            OrderId = ctx.Saga.CorrelationId,
            PaymentId = ctx.Saga.PaymentId,
            ProviderTransactionId = ctx.Saga.ProviderTransactionId,
            Amount = ctx.Saga.TotalAmount,
            Reason = reason
        };

    private UpdateOrderStatusCommand CancelledStatus<T>(BehaviorContext<OrderState, T> ctx, string effect)
        where T : class => new()
        {
            EventId = EffectId(ctx.Saga.CorrelationId, ctx.Message is Domain.Common.BaseMessage message ? message.EventId : Guid.Empty, effect),
            OrderId = ctx.Saga.CorrelationId,
            CustomerName = ctx.Saga.CustomerName,
            CustomerEmail = ctx.Saga.CustomerEmail,
            Status = "Cancelled"
        };

    private CancelOrderCommand CancelOrder<T>(BehaviorContext<OrderState, T> ctx, string effect, string reason)
        where T : class => new()
        {
            EventId = EffectId(ctx.Saga.CorrelationId, ctx.Message is Domain.Common.BaseMessage message ? message.EventId : Guid.Empty, effect),
            OrderId = ctx.Saga.CorrelationId,
            CustomerName = ctx.Saga.CustomerName,
            CustomerEmail = ctx.Saga.CustomerEmail,
            Reason = reason
        };

    private CancelShippingCommand CancelShipping<T>(BehaviorContext<OrderState, T> ctx, string effect)
        where T : class => new()
        {
            EventId = EffectId(ctx.Saga.CorrelationId, ctx.Message is Domain.Common.BaseMessage message ? message.EventId : Guid.Empty, effect),
            OrderId = ctx.Saga.CorrelationId,
            ShippingId = ctx.Saga.ShipmentId
        };

    private string TimeoutReason() => $"Payment not received within {_paymentTimeout:c}";

    private static Guid EffectId(Guid orderId, Guid sourceEventId, string effect)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{orderId:N}:{sourceEventId:N}:{effect}"));
        return new Guid(hash.AsSpan(0, 16));
    }
}
