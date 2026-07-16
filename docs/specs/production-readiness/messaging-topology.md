# Messaging topology

Status: application contract for the later infrastructure-as-code milestone. The machine-readable source is `messaging-topology.json` in this directory.

## Host inventory

| Host | Registration | Endpoints or role |
| --- | --- | --- |
| Basket.EventsProcessor | shared `WithMassTransit` | `products-updated`, `stock-updated`, `empty-basket`, `reinstate-basket` |
| Customers.Messaging | shared, assembly discovery | `customer-address-updated-event` |
| Notifications.Service | shared, assembly discovery | `order-notification`, `seller-notification` |
| Operations.Api | shared, producer only | publishes operations events |
| Operations.Service | shared | `prepare-package`, `operations-seller-stamp` |
| Orders.API | shared, assembly discovery | `order-status-updated-sse` plus producers |
| Orders.Messaging | shared | `cancel-order-command`, `update-order-status-command` |
| Orders.EventsProcessor | shared legacy saga | `submit-order`; saga repository must be explicit |
| Orchestration | special registration migrated to shared transport selection | `order-state`; EF saga repository; scheduled `OrderExpiredEvent` |
| Payments.API | shared, producer only | payment events |
| Payments.gRPC | shared, producer only | payment events |
| Payments.Messaging | shared | `refund-payment` |
| Products.API | shared, producer only | product events |
| Sellers.Api | shared, producer only | seller events |
| Sellers.Service | shared | `seller-orders-submitted`, `seller-document-verification`, `seller-tax-billing-verification` |
| Sellers.EventsProcessor | shared | `seller-sale-registration-requested` |
| Shipping.Api | shared, producer only | shipping events |
| Shipping.Service | shared | `schedule-shipping`, `cancel-shipping`, `order-delivered`, `confirm-shipping` |
| Stock.API | shared, producer only | stock commands/events |
| Stock.gRPC | shared, producer only | stock commands/events |
| Stock.Messaging.Processor | shared | `commit-stock-reservation`, `release-stock-reservation`, `product-published-stock` |

`Orders.Infrastructure.Development.DevelopmentServiceConfiguration` contained an unreferenced in-memory MassTransit host plus a stub publisher. It is not part of the target architecture: the duplicate bus registration is removed while the explicit stub service remains available to its owning development module.

## Consumers and sagas

The inventory contains 24 consumers and two saga registrations:

- Basket: `ProductsEventConsumer`, `StockEventConsumer`, `EmptyBasketCommandConsumer`, `ReinstateBasketCommandConsumer`.
- Customers: `CustomerAddressUpdatedEventConsumer`.
- Notifications: `OrderNotificationConsumer` (two event contracts), `SellerNotificationConsumer`.
- Operations: `PreparePackageCommandConsumer`, `PackageSellerStampConsumer`.
- Orders: `OrderStatusUpdatedSseConsumer`, `CancelOrderCommandConsumer`, `UpdateOrderStatusCommandConsumer`.
- Payments: `RefundPaymentCommandConsumer`.
- Sellers: `OrderSubmittedForSellerConsumer`, `SellerDocumentVerificationConsumer`, `SellerTaxBillingVerificationConsumer`, `OrderSellerSaleRegistrationRequestedConsumer`.
- Shipping: `ScheduleShippingCommandConsumer`, `CancelShippingCommandConsumer`, `OrderDeliveredEventConsumer`, `ConfirmPickupCommandConsumer`.
- Stock: `CommitStockReservationConsumer`, `ReleaseStockReservationConsumer`, `ProductPublishedConsumer`.
- Sagas: the production `OrderStateMachine` with Entity Framework persistence, and the legacy `EventsProcessor.StateMachines.OrderState` saga on `submit-order` with an explicitly selected in-memory repository.

The legacy `Orders.EventsProcessor` project is outside `eShopAcademy.sln` and references two project files that no longer exist (`Orders/Core/Domain/Order.Domain.csproj` and `Orders/Infrastructure/Messaging/Order.Messaging.csproj`). Its registration is included in the contract, but the host cannot be built until that pre-existing project damage is repaired.

## Commands

Commands have one intended destination and should be sent directly when that destination is unambiguous.

| Command | Destination | Producers using `Publish` before this story |
| --- | --- | --- |
| `EmptyBasketCommand` | `empty-basket` | orchestration |
| `ReinstateBasketCommand` | `reinstate-basket` | none found |
| `CommitStockReservationCommand` | `commit-stock-reservation` | orchestration |
| `ReleaseStockReservationCommand` | `release-stock-reservation` | orchestration and order cancellation |
| `ScheduleShippingCommand` | `schedule-shipping` | orchestration |
| `CancelShippingCommand` | `cancel-shipping` | orchestration |
| `ConfirmPickupCommand` | `confirm-shipping` | orchestration |
| `PreparePackageCommand` | `prepare-package` | orchestration |
| `CancelOrderCommand` | `cancel-order-command` | orchestration |
| `UpdateOrderStatusCommand` | `update-order-status-command` | orchestration |
| `RefundPaymentCommand` | `refund-payment` | orchestration and order cancellation |
| `CompleteOrderCommand` | no consumer in repository | orchestration publishes it; intentional unresolved legacy exception |

`ExpireOrderCommand`, `FailOrderCommand`, and `StartOrderPreparationCommand` are public contracts with no producer/consumer in this repository. They remain unchanged for compatibility.

This story converts a command from publish to send only where the manifest has exactly one durable destination and behavior tests cover the route. `CompleteOrderCommand` remains published because no intended endpoint is implemented; silently choosing a destination would change semantics.

## Events and subscriptions

Events are facts and remain published. Durable subscriptions are captured per endpoint in the JSON manifest. The production orchestration saga subscribes to `OrderSubmittedEvent`, `OrderCompletedEvent`, `PaymentCompletedEvent`, `PaymentFailedEvent`, `StockReservationCommittedEvent`, `StockReservationCommitFailedEvent`, `OrderReadyForPickupEvent`, `PackageIssueReportedEvent`, `ShippingFailedEvent`, `ShippingScheduledEvent`, `OrderShippedEvent`, `OrderDeliveredEvent`, and its scheduled `OrderExpiredEvent`.

Published events with no durable consumer in this repository are still valid integration facts and remain public: `OrderCancelledEvent`, `OrderConfirmedEvent`, `OrderFailedEvent`, `OrderPaymentCompletedEvent`, `OrderPaymentFailedEvent`, `PaymentInitiatedEvent`, `PaymentRefundedEvent`, `ShippingCompletedEvent`, `ShippingPickupConfirmedEvent`, `StockReservationCreatedEvent`, `StockReleasedEvent`, `ProductOutOfStockEvent`, and `SellerSaleRegisteredEvent`.

## Scheduler topology

- RabbitMQ: Quartz uses durable endpoint `quartz`; `OrderStateMachine` schedules `OrderExpiredEvent` and stores its scheduling token in saga state.
- Azure Service Bus: the transport-native scheduler is configured; no Quartz endpoint is needed on Service Bus.

No broker-specific entity, exchange, or routing-key name appears in a consumer, saga, application service, or message contract.
