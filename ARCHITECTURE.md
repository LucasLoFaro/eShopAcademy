# Architecture – eShopAcademy

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

