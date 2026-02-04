## 13. Roadmap

In the short-term the solution will be manually deployed to Azure.
Mid-term plans include the development of a simple frontend to showcase the backend features, and the deployment automation.

---

## TODOs

- Production messaging on Azure Service Bus (Section 4.2 / 9). 
	The doc notes RabbitMQ via .NET Aspire is the current local setup and that Azure Service Bus is “intended for production use (WIP),” so the production broker integration still needs implementation.
- Full payments domain with PSP strategy + 3DS support (Section 7). 
	The architecture envisions a strategy-pattern-based payment layer with multiple provider adapters, 3‑D Secure flows, and provider-specific behavior; the current codebase still relies on the fake clients in Orders (FakeClients) and a single payment service, so the extensible payment architecture has yet to materialize.
- Saga compensations/timeouts and notifications on every state change (Sections 5 & 6).
	The canonical flow describes tight choreography—payment timeouts, compensating commands (refund, release stock, etc.), and async notifications per transition—but those steps are still only sketched out; the saga implementation needs to cover each command/event in the document.
- Deployment automation and frontend showcase (Section 13). 
	The roadmap lists manual Azure deployment in the short term and the future rollout of a simple frontend plus deployment automation; those artifacts are not present yet.

### Following tasks

Epic: Complete Order Saga Compensations, Timeouts, and Notifications

Feature 1: Payment completion orchestration
	- Handle PaymentCompletedEvent within the saga. Update the OrderStateMachine so PaymentCompleted transitions the saga, updates persisted order status, and orchestrates the next steps.
	- Emit commands after payment success. From PaymentCompleted, publish CommitStockReservationCommand, ScheduleShippingCommand, and EmptyBasketCommand, ensuring downstream services get the intent while preserving correlation metadata.
	- Finalize success path. After the commands succeed (via events or command responses), publish OrderCompletedEvent and transition the saga to Completed.

Feature 2: Payment failure compensation
	- Handle PaymentFailedEvent. Transition to Failed and log/record the failure reason so diagnostics are available.
	- Trigger compensating workflow. Upon payment failure, publish CancelOrderCommand, then subsequently emit RefundPaymentCommand, ReleaseStockReservationCommand, and ReinstateBasketCommand tied to the same correlation id.

Feature 3: Stock commit/failure flow
	- React to StockReservationCommittedEvent. Once stock is committed, publish PreparePackageCommand and notify operations.
	- React to StockReservationCommitFailedEvent. If the commit fails, invoke CancelOrderCommand to unwind everything, ensuring downstream compensations run.

Feature 4: Timeout & resilience handling
	- Implement payment timeout scheduling. When an order is submitted, schedule a delayed message that fires a PaymentTimeout signal; the saga should handle it by canceling the order if no completion arrives.
	- Idempotent timeout recovery. Guard the timeout handler so it only applies when the saga hasn’t already transitioned to Completed or Failed.

Feature 5: Notifications on state changes
	- Emit domain events for every significant transition. Beyond OrderCompleted, publish events (or invoke a notification service) when the saga enters Submitted, Failed, or when compensations are triggered, allowing the Notification Service to keep customers informed.
	- Hook notification side effects into MassTransit observers/middleware. Ensure the pipeline logs or forwards the events without coupling the saga to notification logic.