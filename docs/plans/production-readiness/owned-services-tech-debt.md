# Owned services production-readiness tech debt

Status: open follow-up work from the Orders, Orchestration, Payments, and PSP production-readiness pass completed on 2026-07-20.

## Scope

This backlog covers unresolved delivery and financial risks in:

- `src/Orders`
- `src/Orchestration`
- `src/Payments`
- `src/PSP`

The completed baseline provides `/alive` and `/health`, shared OpenTelemetry defaults, structured logging, deterministic message correlation, saga transactional outbox behavior, Orders consumer inbox/outbox support, bounded health checks, payment configuration validation, and duplicate suppression within the currently available persistence boundaries.

## Open items

| ID | Priority | Area | Risk | Required follow-up | Acceptance criteria |
|---|---|---|---|---|---|
| OSD-01 | Critical | Payments API and Payments.Messaging | Webhook and refund duplicate suppression is process-local because Payments has no durable store. A restart can forget completed operations and permit the same financial operation to be processed again. | Provision an owned Payments database and replace the in-memory operation registry with a transactional idempotency ledger keyed by operation type plus `PaymentId`, `ProviderTransactionId`, or the provider idempotency key. Apply MassTransit EF inbox/outbox to database-backed payment consumers. | Duplicate payment and refund requests remain single-effect across process restart, redelivery, concurrent delivery, and broker reconnect. The operation result and outgoing event commit atomically. |
| OSD-02 | Critical | Refund processing | `RefundPaymentCommandConsumer` currently publishes `PaymentRefundedEvent` but does not execute or persist a real provider refund. Treating that event as proof of refund would create a financial reconciliation error. | Implement a provider refund workflow with an explicit requested/pending/succeeded/permanently-failed state machine. Persist the provider request and response identifiers without payloads or credentials. | `PaymentRefundedEvent` is emitted only after provider-confirmed success. Transient uncertainty remains pending and is safely reconcilable; permanent business rejection emits a distinct failure outcome. |
| OSD-03 | Critical | PSP/payment configuration | Local AppHost configuration does not supply the PSP simulator with `Payment__SignatureSecret`, and the existing development value used by payment services is shorter than the enforced minimum. Startup will fail once the stronger validation is exercised through AppHost. | Replace hardcoded values with a shared secret resource or user secret of at least 32 characters and inject the same secret into Payments.API, Payments.gRPC, and PSP.Simulator. Define rotation behavior. | All three processes start through AppHost without a literal secret in source or generated manifests; old and new keys can be rotated without accepting unsigned callbacks. |
| OSD-04 | High | Orders.API | Order creation still saves the order before publishing submission and customer-address events. A crash between the database commit and publish can leave an order that never enters the saga. | Add a transactional bus outbox to the order-placement transaction or introduce a durable dispatch record in the Orders database. | A committed order submission is eventually published exactly once from the business perspective after crash/restart; a rolled-back order produces no event. |
| OSD-05 | High | Payment initiation | The PSP idempotency contract is assumed from the local simulator. A real provider may ignore the `Idempotency-Key`, scope it differently, or return an ambiguous timeout after charging. Retrying remains disabled, but callers still need reconciliation for uncertain outcomes. | Document and verify the production PSP idempotency contract. Persist initiation attempts and add provider-status reconciliation before allowing any retry after timeout or transport failure. | Automated provider-contract tests prove stable-key behavior. An ambiguous timeout cannot create a second charge and resolves to a durable final or manual-review state. |
| OSD-06 | High | Health policy | Payments.gRPC currently treats PSP availability as readiness-critical because initiating payments is its only workload. The broader platform contract says external providers should normally remain outside readiness to avoid cascading eviction during provider outages. | Make an explicit platform decision. Prefer startup validation plus request-time circuit/availability metrics unless the deployment owner confirms PSP-driven unready behavior is intentional. | The documented dependency matrix, implementation, probes, and incident runbook agree. A PSP outage cannot make unrelated payment endpoints or the refund worker unready. |
| OSD-07 | High | Worker deployment | Orders.Messaging, Orchestration, and Payments.Messaging now expose local management listeners on ports 8004, 8005, and 8007, but shared AppHost, deployment manifests, and infrastructure probes were intentionally not changed. | Declare private management endpoints and liveness/readiness probes in AppHost and every deployment target. Do not expose worker management ports publicly. | Each deployed worker has reachable private `/alive` and `/health` probes with no port collision; probes are exercised in deployment smoke tests. |
| OSD-08 | High | Saga system verification | Five PostgreSQL/RabbitMQ system tests could not run because the configured `podman-machine-default` Docker context does not exist. These tests cover restart recovery, `xmin` concurrency, timeout races, duplicate replay, and persisted outbox dispatch. | Repair the CI/local container runtime configuration and run with `RUN_ORDER_SAGA_SYSTEM_TESTS=true`. | All five containerized tests pass in CI and on the supported developer runtime; failures publish actionable container diagnostics. |
| OSD-09 | Medium | PSP simulator | Simulator payment and resolution state is in memory. Restarting it loses pending payments and duplicate history, which can invalidate long-running local/system tests. | Either document the simulator as ephemeral and reset dependent tests deliberately, or add lightweight durable test storage keyed by external order ID. | Restart behavior is deterministic and covered by a test; duplicate order initiation after restart cannot silently create inconsistent test outcomes. |
| OSD-10 | Medium | Observability export | Services register shared OpenTelemetry sources and bounded-cardinality meters, but Application Insights ingestion still depends on shared OTLP/Azure Monitor configuration outside the owned contexts. | Configure the platform OTLP collector or Azure Monitor exporter, resource attributes, sampling, and dashboards/alerts for payment results, retries, message faults, and saga failures. | Telemetry from all seven executables is visible in Application Insights with service, trace, order correlation, and bounded metric dimensions; exporter failure never affects readiness. |
| OSD-11 | Medium | Messaging metrics | Business-result metrics exist, but retry and fault counters are not yet driven by a common MassTransit receive observer for every Orders and saga endpoint. Some framework-level faults may only appear in MassTransit telemetry. | Add a shared receive/retry observer or approved middleware that records endpoint, message type, outcome, and bounded failure category without payload data. | Forced consumer exceptions and exhausted retries increment the expected metrics once per attempt/fault without order IDs, exception messages, or payload-derived tags. |
| OSD-12 | Medium | Shared web defaults | Swagger, CORS, ProblemDetails, forwarded headers, sanitized health responses, and health timeout budgets are partly applied per executable because shared defaults were out of scope. This can drift across bounded contexts. | Consolidate the approved behavior in `ServiceDefaults` after platform review, including trusted proxy configuration and the production management-listener policy. | Architecture tests prove every applicable owned executable uses the shared policy and cannot expose permissive CORS, detailed exceptions, or untrusted forwarded headers by default. |

## Required validation before closing critical items

- Duplicate payment initiation, webhook, and refund delivery across process restart.
- Concurrent delivery of the same stable business key from separate broker messages.
- Provider timeout after an accepted charge or refund, followed by reconciliation.
- Permanent provider/business rejection with no automatic retry.
- Database commit followed by process termination before broker dispatch.
- Broker outage followed by recovery and outbox drain.
- Secret rotation and invalid/missing configuration startup tests.
- Log and telemetry canary tests containing card-like data, credentials, customer data, connection strings, and complete payloads.

## Ownership and sequencing

1. Platform owners: OSD-03, OSD-06, OSD-07, OSD-08, OSD-10, and OSD-12.
2. Payments owners: OSD-01, OSD-02, OSD-05, and the Payments portion of OSD-11.
3. Orders owners: OSD-04 and the Orders portion of OSD-11.
4. PSP/test-infrastructure owners: OSD-09 and provider-contract test support for OSD-05.

OSD-01 through OSD-05 should be completed before enabling real-money payment or refund processing. OSD-08 should be completed before treating the saga reliability work as fully release-verified.
