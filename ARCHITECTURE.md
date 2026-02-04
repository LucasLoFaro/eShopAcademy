# 🏗️ Architecture – eShopAcademy

## 1. Purpose

This document describes the high-level architecture of eShopAcademy, a .NET 8
microservices-based e-commerce platform.

The project is intended as a real-world reference implementation, prioritizing

- Architectural correctness
- Clear service boundaries
- Maintainability and extensibility

This is not a simplified demo or monolithic application.

---

## 2. Architectural Style

- Microservices architecture
- Clean Architecture  DDD-inspired layering
- Top-down design
  - Contracts first
  - Infrastructure last
- Event-driven workflows where appropriate

Each service is

- Independently deployable
- Responsible for its own data
- Integrated only through explicit contracts

---

## 3. Services & Boundaries

Each microservice is implemented as an independent .NET project within the same
repository.

Typical services include

- Order Service
- Payment Service
- Stock Service
- Basket Service
- Shipping Service
- Notification Service
- Operations Service

### Boundary Rules

- No shared databases
- No cross-service ORM access
- Data duplication is acceptable to preserve autonomy

---

## 4. Communication Patterns

### 4.1 Synchronous Communication

- gRPC is used for internal service-to-service calls
- Used when
  - An immediate response is required
  - Strong consistency is needed

- HTTPS REST APIs are used for external communication between the frontend and backend services via an API Gateway

Examples
gRPC:
- Order -> Payment 
- Order -> Stock

Https:
- Frontend -> API Gateway -> Basket
- Frontend -> API Gateway -> Product

---

### 4.2 Asynchronous Communication

- Azure Service Bus is used for messaging on Production
- RabbitMQ is used for messaging on Local Development (via .NET Aspire)
- Messaging is the preferred mechanism for
  - Integration events
  - Workflow progression
  - Eventual consistency
  - Decoupling services

Message semantics are explicit

- Commands express intent and target a single consumer
- Events represent facts and may have multiple consumers

Examples
- Shipping -> Order.Orchestration (SAGA)
- PaymentNotification -> Order.Orchestration (SAGA)


---

## 5. Order Workflow (Canonical Flow)

The order lifecycle is orchestrated using the Saga pattern (MassTransit),
with the Order Service acting as the main orchestrator.

### Canonical Order Flow

Synchronous Steps before starting SAGA orchestration:
On PlaceOrder: 
	- Payment is initiated
	- Basket is validated
	- Stock is reserved

Orders SAGA:
On OrderSubmittedEvent:
	On PaymentCompletedEvent:
		- CommitStockReservationCommand
		- ScheduleShippingCommand
		- EmptyBasketCommand
	On PaymentFailedEvent: CancelOrderCommand 

On StockReservationCommittedEvent:
	- PreparePackageCommand (Operations)

On StockReservationCommitFailedEvent:
	- CancelOrderCommand

On OrderReadyForPickupEvent:
	- ConfirmShippingCommand

On ShippingCompletedEvent:
	- CompleteOrderCommand

On CancelOrderCommand:
	- RefundPaymentCommand
	- ReleaseStockReservationCommand
	- CancelShippingCommand
	- ReinstateBasketCommand

Notifications are sent async on every significant state change without blocking the workflow.
Each payment has a timeout to cancel the order if not completed in time.

---

## 6. Workflow & Orchestration (Saga Pattern)

- Long-running business workflows are coordinated using sagas
- Sagas are responsible for:
  - State management
  - Success and failure handling
  - Compensating actions

### Key Principles

- Orchestration is explicit (not implicit choreography)
- Failures trigger compensations (e.g. releasing stock)
- Timeouts are first-class concerns
- Sagas always end in a clear terminal state

---

## 7. Payment Architecture

Payments are treated as a **complex, isolated domain**.

### Characteristics

- Strategy pattern for payment providers (PSPs)
- Each PSP is implemented as an adapter
- Support for:
  - Multiple providers
  - 3DS flows
  - Provider-specific behavior

### Design Goals

- No PSP logic leaking into domain code
- Easy extensibility for new providers
- Clear separation between orchestration and execution

---

## 8. Data & Persistence

### Storage Technologies

- **PostgreSQL**
  - Primary relational data store

- **Redis**
  - Caching
  - Idempotency
  - Temporary workflow state

- **Cosmos DB**
  - Document-oriented use cases

### Data Ownership Rules

- Each service owns its persistence
- No direct cross-service database access
- Data duplication is acceptable when justified

---

## 9. Local Development

- **.NET Aspire** is used for local orchestration
- Responsibilities include:
  - Container wiring
  - Dependency discovery
  - Local observability

RabbitMQ is used for local development through Aspire. Azure Service Bus is intended for production use (WIP).

All services must be runnable locally with minimal setup.
Whenever a nuget project is updated, the nugets must be packed, published to the artifact feed and all consumers should be updated to the last version.
Push command: dotnet nuget push --source Github .\packages\ --skip-duplicate

---

## 10. Testing Strategy

- **Unit tests**
  - Domain logic
  - Application behavior

- **Integration tests**
  - Messaging
  - Persistence

- **System / UI tests**
  - Playwright where applicable

### Testing Principles

- Prefer real serialization over mocks
- Validate contracts explicitly
- Test workflows, not only isolated units

---

## 11. Architectural Principles

### Strongly Enforced

- Loose coupling
- High cohesion
- Explicit contracts
- Eventual consistency

### Explicitly Avoided

- God services
- Shared persistence
- Hidden dependencies
- “Just make it work” shortcuts

---

## 12. Final Notes

If a design decision:

- Improves clarity → preferred
- Reduces coupling → preferred
- Trades simplicity for correctness → acceptable

When in doubt, **follow existing patterns in the codebase**.


---

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