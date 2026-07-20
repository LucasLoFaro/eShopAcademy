# Order saga reliability

## Scope

This specification defines the production reliability boundary for the order saga. The saga remains a MassTransit state machine persisted by the Entity Framework PostgreSQL repository in `OrderSagaDbContext`. It does not replace the repository with an in-memory, document, or transport-native saga store.

The correctness boundary consists of the saga row, the MassTransit Entity Framework transactional outbox tables in the same PostgreSQL database, and the durable scheduler selected by the configured transport. Outgoing consumers remain responsible for making their own business effects idempotent.

## Required guarantees

### Saga-state persistence

Every accepted event persists its state transition in `OrderSagaDbContext` before the transport delivery is acknowledged. A process failure before commit leaves the previous state and permits redelivery. A process failure after commit leaves the new state durable. The saga is correlated by `OrderId`, which is also its `CorrelationId`.

### Scheduled-message persistence

The payment timeout is submitted to the configured durable scheduler: a clustered Quartz PostgreSQL ADO job store for RabbitMQ and native scheduling for Azure Service Bus. Quartz uses the orchestration database and its schema is deployed by the saga durability migration. The schedule request is written through the saga's transactional outbox. A committed order submission therefore eventually creates a timeout even if the orchestration process stops before dispatching it.

The configured payment timeout duration is the single source of truth for both the scheduler delay and `OrderExpiredEvent.ExpiredAt`. The duration must be positive and is validated during host startup.

Unscheduling after payment is best-effort cleanup only. Correctness never depends on successful unscheduling: a delivered timeout is actionable only while the saga is payment-pending and is ignored in every later state.

### Atomicity between saga updates and outgoing messages

Saga changes and saga-produced sends, publishes, and schedule/unschedule requests are captured in the MassTransit Entity Framework transactional outbox in the same `OrderSagaDbContext` transaction. A saga transition cannot commit while silently losing its outgoing messages. After a crash, the outbox delivery service resumes dispatch from the persisted rows.

This is transactional atomicity and at-least-once dispatch, not global exactly-once delivery. A crash or broker acknowledgement loss can still cause a persisted outbox message to be delivered more than once.

### Duplicate delivery

The state machine rejects duplicate business transitions by state. `PaymentCompletedEvent` is accepted only while payment is pending and moves the saga to a distinct processing state before producing paid-order effects. Shipping-scheduled, stock-committed, pickup-ready, shipped, and delivered events are accepted only in states where their business effect has not already occurred. Replays in later or terminal states are ignored and produce no commands.

Every saga-produced command carries the stable `OrderId` plus its applicable business identifier (`PaymentId`, `ReservationId`, `BasketClientId`, or `ShipmentId`). MassTransit preserves the initiating conversation/correlation metadata. Those identifiers let consumers deduplicate a logical operation even when an envelope is redelivered.

### Idempotent compensation

The saga emits each compensation path only from the state in which that failure is actionable and moves immediately to a terminal state. Duplicate failure, timeout, or issue events cannot cause the saga to emit the same compensation twice.

This prevents repeated *production* of compensating commands by a persisted saga transition. It does not make a downstream refund, stock release, basket mutation, shipping cancellation, or order update exactly-once. Each consumer must persist a unique operation key and return its prior result when that key is replayed.

Recommended consumer keys are:

| Effect | Stable business key |
|---|---|
| Commit or release reservation | `OrderId + ReservationId + operation` |
| Capture or refund payment | `OrderId + PaymentId + operation` |
| Empty or reinstate basket | `OrderId + BasketClientId + operation` |
| Schedule, confirm, or cancel shipping | `OrderId + ShipmentId + operation` (use `OrderId + schedule` before a shipment exists) |
| Update, cancel, or complete order | `OrderId + target status/operation` |

### Optimistic concurrency

The Entity Framework saga repository uses optimistic concurrency. Concurrent deliveries for the same `OrderId` cannot both commit from the same prior version. The loser must be retried/redelivered against the newly persisted state, where state-based duplicate guards determine whether it is still actionable.

The PostgreSQL mapping uses a real database-generated concurrency token; application code must not generate or overwrite it. Concurrency-conflict tests must use PostgreSQL semantics rather than relying only on the in-memory test harness.

### Terminal-state behavior

Completed and failed sagas are logical terminal tombstones and are deliberately retained. Late submission, payment, timeout, stock, shipping, delivery, and failure events are ignored and cannot recreate or reactivate a finalized instance. Retention closes the cross-lifetime duplicate-submission gap for the same `OrderId`; any future cleanup policy must archive an equivalent durable terminal/deduplication record before deleting a saga row.

## State and timeout invariants

- A submitted order is payment-pending.
- `PaymentCompletedEvent` is actionable only while payment is pending and transitions the saga to processing before emitting stock, basket, shipping, and paid-status effects.
- `OrderExpiredEvent` is actionable only while payment is pending.
- A timeout delivered in processing, shipped, completed, or failed state is ignored.
- A paid order can never be cancelled by a stale payment timeout.
- Shipping and delivery progress is monotonic; duplicate or older events cannot move the saga backward.
- Finalization is one-way; a finalized saga cannot return to an active state.
- Timeout duration and the event's `ExpiredAt` value come from the same validated options value and the same time observation.

## Logging and data handling

State-machine diagnostics use structured `ILogger<OrderStateMachine>` messages with identifiers as named properties. Logs must not include customer email, customer name, postal address, provider payloads, or message bodies.

Entity Framework sensitive-data logging is disabled by default in every environment. It may be enabled only by an explicit local-development configuration flag and must never be inferred solely from the environment name.

## Verification

Deterministic tests cover timeout-before-payment, payment-before-timeout, stale timeout after payment, duplicate payment, duplicate shipping and delivery progress, payment/timeout races, optimistic concurrency conflict handling, failure after saga persistence but before dispatch, restart recovery of pending outbox rows, and ignored events after finalization.

Harness tests validate state-machine behavior and produced messages. Provider-model and migration-script checks validate PostgreSQL concurrency and storage shape; transaction and restart tests validate rollback/commit boundaries and outbox recovery. Production smoke tests must additionally apply the migration and exercise scheduler recovery for each configured transport because an in-memory harness cannot prove broker or scheduler persistence.
