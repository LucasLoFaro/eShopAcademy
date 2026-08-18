# Owned services production-readiness tech debt

Status: authoritative production-readiness status ledger, refreshed 2026-08-18.

Scope: `src/Stock`, `src/Shipping`, `src/Operations`, `src/Notifications`, and `src/Gateway`, including simulators in those directories. This file records work that remains after the owned executables received health endpoints, telemetry, configuration validation, retry conventions, and initial idempotency coverage. It does not authorize changes to shared platform files or other bounded contexts.

## Ledger policy

This document is the authoritative status ledger for the production-readiness program. Other readiness documents describe contracts, inventories, or implementation plans; when their wording conflicts with an item's status here, this ledger controls.

- **Open:** acceptance evidence is incomplete.
- **In progress:** a named delivery PR is active, but acceptance evidence or required traceability is incomplete.
- **Proven:** the stated runtime or automated check passed in the recorded environment. Proven evidence can satisfy part of an open item's acceptance criteria without closing the item.
- **Closed:** every acceptance criterion has linked runtime or automated evidence and the implementation is linked through its issue and delivery PR.
- Documentation, code review, a successful build, or a passing unit test cannot close an item that requires representative infrastructure, fault injection, restart recovery, or a process-crash boundary.
- An item with an unfiled issue, unopened delivery PR, or missing evidence remains open.

## PR-00 readiness baseline

| Readiness claim | Status | Environment and evidence | Remaining work |
| --- | --- | --- | --- |
| Full-topology startup | Proven | On 2026-08-18, the complete Aspire topology started successfully on Windows with Podman 5.8.3. This is runtime evidence for startup only. | Preserve a repeatable run record in the delivery issue/PR when topology work is closed. |
| Dependency failure and recovery | Open | No complete runtime evidence currently records independent failure and recovery for every critical database, broker, storage, and provider dependency. | Inject each dependency failure, record `/alive` and `/health`, recovery timing, isolation, and sanitized telemetry. |
| Podman 6.0.x on Windows | Unsupported | A forwarding regression prevents Podman 6.0.x from being treated as a supported Windows runtime for this repository. | Revalidate after the upstream forwarding regression is resolved; do not use 6.0.x evidence to close readiness work before then. |

## Delivery sequence

Shared platform and contract changes are delivered sequentially. PR-01 is intentionally skipped for now; starting PR-02 does not remove PR-01 as a prerequisite for later context reliability work.

| Planned PR | Owner | Status | Scope | Blocks |
| --- | --- | --- | --- | --- |
| PR-01 — Durable messaging primitives | Shared messaging platform | Planned; skipped for now | MongoDB and EF inbox/outbox registration; stable operation/effect identifiers; failure classifications; bounded retry/redelivery; duplicate, backlog, retry, and fault instrumentation; RabbitMQ/Azure Service Bus parity tests. | Most context reliability PRs. |
| PR-02 — Shared product deletion contract | Shared domain contracts | In progress | Versioned `ProductDeletedEvent`; stable product and correlation metadata; contract compatibility tests. | Products/Basket deletion behavior. |
| PR-03 — Shared seller attribution contract | Shared order contracts | In progress | Authoritative seller identity; stable seller-sale operation identifier; validation and serialization compatibility tests. | Deterministic seller routing. |
| PR-04 — Private worker management endpoints | AppHost/deployment platform | In progress | Unique private `/alive` and `/health` endpoints and readiness probes for Basket, Customers, Sellers, Stock, Shipping, Operations, Notifications, Orders, Orchestration, and Payments workers. | Worker deployment and full-topology readiness validation. |
| PR-05 — Production configuration and secrets | Platform security | Planned | Shipping and PSP signature secrets; shared option validation; production-safe web and service-authentication defaults; secret/PII telemetry canaries. | Production configuration and security validation. |

### PR-04 runtime evidence

On 2026-08-18, an isolated Aspire run on Windows with Podman client 5.8.3 and machine 5.8.6 reported all eleven worker resources `Running` and `Healthy`. Aspire reported a healthy `/health` check for every endpoint below.

| Worker | Private endpoint |
| --- | --- |
| Basket EventsProcessor | `http://localhost:8101` |
| Customers Messaging | `http://localhost:8102` |
| Sellers Service | `http://localhost:8103` |
| Sellers EventsProcessor | `http://localhost:8104` |
| Stock Messaging | `http://localhost:8105` |
| Shipping Service | `http://localhost:8106` |
| Operations Service | `http://localhost:8107` |
| Notifications Service | `http://localhost:8108` |
| Orders Messaging | `http://localhost:8109` |
| Orchestration | `http://localhost:8110` |
| Payments Messaging | `http://localhost:8111` |

This proves endpoint publication, unique port assignment, and healthy-path readiness probing. It does not prove dependency failure isolation or recovery, so the mapped debt items remain in progress.

## Priority definitions

- **P0**: a production rollout can expose a security, correctness, or availability failure; resolve before rollout.
- **P1**: controls exist, but realistic infrastructure or crash-boundary verification is incomplete; schedule before broad production traffic.
- **P2**: maintainability or assurance gap with a lower immediate operational impact.

## Issue and PR traceability

PR-00 establishes the ledger and baseline only; it does not deliver or close any debt item. The repository currently has no dedicated GitHub issues or delivery PRs for the items below. That missing linkage is recorded explicitly and blocks closure rather than being replaced with unrelated historical links.

| Tech-debt item | Status | GitHub issue | Delivery PR | Origin |
| --- | --- | --- | --- | --- |
| TD-OWNED-001 | In progress | Not filed | PR-04; runtime evidence open | PR #40 |
| TD-OWNED-002 | Open | Not filed | Planned PR-05 | PR #40 |
| TD-OWNED-003 | Open | Not filed | Not opened; blocked by PR-01 | PR #40 |
| TD-OWNED-004 | Open | Not filed | Not opened; blocked by PR-01 | PR #40 |
| TD-OWNED-005 | Open | Not filed | Not opened; blocked by PR-01 | PR #40 |
| TD-OWNED-006 | Open | Not filed | Planned PR-01 | PR #40 |
| TD-OWNED-007 | In progress | Not filed | PR-04 endpoints; failure/recovery PR not opened | PR #40 |
| TD-OWNED-008 | Open | Not filed | Not opened; partially blocked by PR-05 | PR #40 |
| TD-OWNED-009 | Open | Not filed | Not opened; partially blocked by PR-05 | PR #40 |
| TD-OWNED-010 | Open | Not filed | Not opened | PR #40 |
| TD-BCPS-001 | Open | Not filed | Not opened; blocked by PR-01 | PR #43 |
| TD-BCPS-002 | In progress | Not filed | PR-02 contract; context PR not opened | PR #43 |
| TD-BCPS-003 | In progress | Not filed | PR-03 contract; routing PR not opened | PR #43 |
| TD-BCPS-004 | Open | Not filed | Not opened; blocked by PR-01 | PR #43 |
| TD-BCPS-005 | In progress | Not filed | PR-04; runtime evidence open | PR #43 |
| TD-BCPS-006 | Open | Not filed | Not opened; partially blocked by PR-05 | PR #43 |
| TD-BCPS-007 | Open | Not filed | Not opened | PR #43 |
| TD-BCPS-008 | Open | Not filed | Not opened | PR #43 |
| TD-BCPS-009 | Open | Not filed | Not opened | PR #43 |
| OSD-01 | Open | Not filed | Not opened; blocked by PR-01 | PR #42 |
| OSD-02 | Open | Not filed | Not opened | PR #42 |
| OSD-03 | Open | Not filed | Planned PR-05 | PR #42 |
| OSD-04 | Open | Not filed | Not opened; blocked by PR-01 | PR #42 |
| OSD-05 | Open | Not filed | Not opened; blocked by PR-01 | PR #42 |
| OSD-06 | Open | Not filed | Not opened | PR #42 |
| OSD-07 | In progress | Not filed | PR-04; runtime evidence open | PR #42 |
| OSD-08 | Open | Not filed | Not opened | PR #42 |
| OSD-09 | Open | Not filed | Not opened | PR #42 |
| OSD-10 | Open | Not filed | Not opened | PR #42 |
| OSD-11 | Open | Not filed | Planned PR-01 | PR #42 |
| OSD-12 | Open | Not filed | Planned PR-05 | PR #42 |

## To-do

### TD-OWNED-001 — Publish unique worker health endpoints from AppHost (P0)

**Risk:** The Stock processor, Shipping service, Operations service, and Notifications service host internal `/alive` and `/health` HTTP listeners, but the shared AppHost does not yet declare unique HTTP endpoints for them. Orchestration probes and operators may therefore be unable to reach or distinguish worker health endpoints.

**Work:** Update the shared AppHost to allocate and publish one unique health endpoint per worker. Do not route worker probes through the Gateway.

**Acceptance criteria:**

- Each worker has a unique, reachable health URL in an Aspire run.
- `/alive` remains self-only.
- `/health` reports only the worker's owned broker and persistence dependencies.
- Stopping one worker or one owned dependency changes only the expected readiness result.

**Owner:** Shared platform/AppHost. The owned services should retain their existing endpoint contracts.

### TD-OWNED-002 — Remove the shipping signature secret from AppHost configuration (P0)

**Risk:** The shipping simulator signature secret is hardcoded in shared AppHost configuration. A committed secret cannot be safely rotated and can make non-production conventions leak into deployed environments.

**Work:** Move the value to the platform secret/configuration mechanism and rotate the existing value. Keep signature or credential values out of logs, traces, health payloads, and exception messages.

**Acceptance criteria:**

- No shipping signature secret is present in tracked AppHost source or generated configuration.
- Startup fails with a sanitized configuration error when the secret is missing or invalid.
- Rotation is documented and verified without rebuilding the service.

**Owner:** Shared platform/AppHost, coordinated with Shipping.

### TD-OWNED-003 — Prove MongoDB inbox/outbox behavior on the deployment topology (P0)

**Risk:** Database-backed Stock, Shipping, Operations, and Notifications consumers rely on MongoDB transaction behavior for atomic inbox/outbox guarantees. Unit and harness tests do not prove that the production MongoDB topology supports the required transactions or that crash recovery produces one business effect.

**Work:** Run fault-injection integration tests against the same replica-set or managed-cluster topology used in deployment. Cover crashes before and after state commit, inbox completion, and outbox dispatch.

**Acceptance criteria:**

- Deployment validation rejects a MongoDB topology that cannot support the required transaction semantics.
- Replaying the same `EventId` and stable business identifier produces one stock, reservation, shipment, package, or notification state transition.
- A process crash at each persistence/publish boundary eventually dispatches one logical outcome without losing the state change.
- Recovery and outbox backlog are observable without exposing payloads.

**Owner:** Owned contexts for tests; shared platform/deployment for the representative MongoDB environment.

### TD-OWNED-004 — Add recoverable idempotency operation states (P1)

**Risk:** Current idempotency guards favor duplicate safety by claiming a key before performing the protected mutation. A crash after the claim but before completion can leave an operation blocked, and a duplicate request cannot always return the original result.

**Work:** Replace claim-only records for critical stock and shipping operations with a durable operation lifecycle such as `Pending`, `Completed`, and `Failed`, including a bounded lease or reconciliation path and a sanitized stored outcome.

**Acceptance criteria:**

- An abandoned `Pending` operation can be reconciled or safely resumed after its lease expires.
- A completed duplicate returns the prior logical outcome without repeating stock changes or provider calls.
- Parallel duplicates converge on one operation owner and one terminal result.
- Operators can identify stuck operations by age and operation type using bounded-cardinality telemetry.

**Owner:** Stock and Shipping.

### TD-OWNED-005 — Close the external email exactly-once gap (P1)

**Risk:** A Notifications process can crash after SendGrid accepts an email but before the local inbox operation is marked complete. Broker redelivery can then send the same email again. Local database idempotency alone cannot prove exactly-once delivery across this provider boundary.

**Work:** Confirm and adopt a provider-supported idempotency contract, or introduce a durable delivery ledger and reconciliation workflow keyed by a stable notification identifier. Treat timeout-after-submit as an unknown outcome, not an automatic new send.

**Acceptance criteria:**

- A stable notification/delivery identifier is preserved across retries and restarts.
- Timeout-after-accept and crash-after-accept tests do not produce multiple customer-visible emails.
- Permanent provider rejection reaches a terminal, observable state without making unrelated consumers unready.
- Logs and telemetry contain no email address, customer data, credentials, or complete provider payload.

**Owner:** Notifications, with provider-contract confirmation from the platform/integration owner.

### TD-OWNED-006 — Exercise real broker-outage and transport-parity scenarios (P1)

**Risk:** MassTransit bus health and consumer retry behavior are covered by local tests, but readiness and redelivery have not been verified against an actually unavailable RabbitMQ and the production Azure Service Bus transport. Transport-specific behavior could differ from the harness.

**Work:** Add container or environment-backed tests for startup outage, mid-flight disconnect, recovery, retry exhaustion, error transport, and graceful shutdown on every supported broker transport.

**Acceptance criteria:**

- Broker loss makes worker `/health` unhealthy within a bounded interval while `/alive` remains healthy.
- Transient failures recover within the declared attempt budget; permanent failures are attempted once and routed deterministically.
- Correlation, trace, `EventId`, and stable business identifiers survive retry and redelivery.
- No retry repeats an unsafe provider operation without an idempotency key.

**Owner:** Shared messaging platform for transport fixtures and policy parity; owned contexts for representative consumers.

### TD-OWNED-007 — Complete Aspire runtime and endpoint isolation verification (P1)

**Risk:** Full-topology startup is proven on Windows with Podman 5.8.3, but startup alone does not prove endpoint publication, dependency-failure behavior, bounded timeouts, recovery, or Gateway readiness isolation across the deployed process graph.

**Work:** After worker endpoints and required configuration are supplied by AppHost, run the complete owned process set and inject database, broker, storage, and notification-provider failures independently.

**Acceptance criteria:**

- Every executable exposes reachable `/alive` and `/health` endpoints.
- Gateway readiness checks only its critical infrastructure and does not fan out to every downstream service.
- A notification-provider outage does not make unrelated consumers or the Gateway unready.
- Health responses and logs remain non-sensitive under failure.

**Owner:** Shared AppHost/platform for orchestration; owned contexts for assertions and fault scenarios.

### TD-OWNED-008 — Centralize shared web and Azure Monitor defaults (P1)

**Risk:** Owned APIs can register local ProblemDetails, CORS, proxy, and OpenTelemetry behavior, but configuration can drift. The shared defaults currently do not provide the complete Azure Monitor/Application Insights exporter path required for consistent production telemetry.

**Work:** Extend `ServiceDefaults` through a separately reviewed shared-platform change to provide the approved ProblemDetails, named CORS policy, forwarded-header/proxy behavior, and Azure Monitor exporter configuration. Migrate owned services only after the shared contract is available.

**Acceptance criteria:**

- APIs use one documented shared registration path with environment-safe defaults.
- Azure Monitor export can be enabled by validated configuration without code changes.
- Missing optional telemetry export does not fail service startup; invalid critical settings fail with sanitized errors.
- Trace and order correlation remain intact across HTTP, gRPC, and MassTransit boundaries.

**Owner:** Shared platform. No shared-file change is part of the owned-context implementation.

### TD-OWNED-009 — Expand process-level health and sensitive-logging tests (P2)

**Risk:** Representative endpoint and logging tests cover the common behavior, but they do not launch every executable under every dependency failure. A process-specific registration or future log template could regress without being detected.

**Work:** Add a data-driven process matrix that launches every owned executable and validates both health endpoints, dependency isolation, timeout bounds, configuration failures, and a deny-list of sensitive fields in captured logs.

**Acceptance criteria:**

- Every executable appears in the test matrix and both endpoint contracts are asserted.
- Database, broker, storage, and provider failures are tested wherever the dependency is owned and critical.
- Captured logs are checked for credentials, connection strings, addresses, customer fields, signatures, and complete message bodies.
- The matrix runs in CI with deterministic ports and bounded execution time.

**Owner:** Stock, Shipping, Operations, Notifications, and Gateway.

### TD-OWNED-010 — Remove owned-project build warnings (P2)

**Risk:** Existing nullable warnings in Stock and redundant hosting package references can conceal new warnings and make warning-based quality gates less useful.

**Work:** Correct the nullable contracts in the affected Stock data/domain paths and remove direct `Microsoft.Extensions.Hosting` references when they are supplied by the SDK or shared dependency graph. Package-version changes must remain a shared-platform request when they require `Directory.Packages.props`.

**Acceptance criteria:**

- All owned projects build without nullable or `NU1510` warnings.
- No central package file is modified as part of bounded-context cleanup.
- Existing behavior and public contracts remain covered by tests.

**Owner:** Owned contexts; shared dependency owner only if central version changes are required.

## Completion evidence required

Close an item only with links to the implementation and its automated or runtime evidence. Record the broker and database topology, failure injected, expected result, actual result, and any sanitized telemetry used to confirm recovery. A passing unit test alone is insufficient for items that explicitly require representative infrastructure or a process-crash boundary.

## Basket, Customers, Products, and Sellers follow-up

The following items remain after rebasing the Basket, Customers, Products, and Sellers hardening work onto the shared observability and resilience foundation. The upstream Azure Monitor exporter and safe-method HTTP retry policy close the previously recorded telemetry-export and unsafe-default-retry gaps, so those are not repeated as open debt.

### TD-BCPS-001 — Adopt durable MongoDB inbox/outbox semantics (P0)

**Risk:** Customers Messaging, Sellers Service, and Sellers EventsProcessor currently use process-local outbox protection. Products API and Sellers API also have database-write-plus-publish flows without a durable atomic handoff. A crash can lose or duplicate an event even though ordinary duplicate delivery is handled.

**Work:** Use the shared context-aware endpoint registration to adopt the approved durable MongoDB inbox/outbox convention. Define atomic or recoverable database-to-message handoffs for API publication paths.

**Acceptance criteria:** Duplicate delivery is suppressed across restarts and concurrent replicas; crashes at each persistence/publish boundary recover to one logical business result; inbox and outbox backlog are observable without payload logging.

**Owner:** Customers, Products, and Sellers with the shared messaging/platform owner for the MongoDB persistence convention.

### TD-BCPS-002 — Introduce an explicit product-deletion event (P0)

**Risk:** `ProductMessagingService.SendProductDelete` publishes `ProductUpdatedEvent` because the shared domain has no deletion event. Basket cannot distinguish deletion from update, so stale product cache entries can survive.

**Work:** Add a shared `ProductDeletedEvent` carrying a stable product identifier, publish it after product deletion, and make Basket evict the corresponding cache entry idempotently. Document the contract as an event/fact rather than a command.

**Acceptance criteria:** Product deletion publishes one logical deletion fact; update and deletion are distinguishable; duplicate deletion delivery is harmless; Basket cache eviction is covered by contract and integration tests.

**PR-02 evidence:** [`ProductDeletedEvent`](../../../src/Domain/Common.Domain/Events/Products/ProductDeletedEvent.cs) introduces the additive version-one contract, and [`ProductDeletionContractTests`](../../../src/Products/Tests/Tests/ProductDeletionContractTests.cs) pins its identity, correlation metadata, serialization, and distinction from `ProductUpdatedEvent`. Products publication and Basket eviction remain for the blocked context PR, so this item is not closed.

**Owner:** Shared domain-contract owner with Products and Basket.

### TD-BCPS-003 — Define authoritative seller attribution for submitted orders (P1)

**Risk:** The Sellers `OrderSubmitted` consumer cannot complete a deterministic seller transition when seller attribution is absent or ambiguous. Seller workflow state can remain incomplete while the message is acknowledged.

**Work:** Define authoritative seller attribution fields and a stable business identifier in the shared order contract, then implement the seller-side transition and failure classification.

**Acceptance criteria:** Every eligible order item maps to one seller; replay cannot duplicate work; missing or invalid attribution follows a documented permanent or business-error path.

**PR-03 evidence:** [`OrderItemSellerAttribution`](../../../src/Domain/Common.Domain/Events/Orders/OrderItemSellerAttribution.cs) adds authoritative eligible-item seller metadata to `OrderSubmittedEvent`; [`SellerAttributionContract`](../../../src/Domain/Common.Domain/Events/Orders/SellerAttributionContract.cs) defines deterministic seller-sale operation identity and validation; and [`SellerAttributionContractTests`](../../../src/Orders/Tests/Messaging/SellerAttributionContractTests.cs) covers replay stability, invalid attribution, and old/new serialization compatibility. Deterministic seller routing remains for the blocked context PR, so this item is not closed.

**Owner:** Shared order-contract owner with Sellers.

### TD-BCPS-004 — Make seller verification concurrency-safe (P1)

**Risk:** Document and tax/billing verification consumers avoid ordinary sequential duplicates, but concurrent replicas can race without a durable inbox and atomic compare-and-set transition. External verification can be repeated and terminal state overwritten.

**Work:** Key durable inbox records by seller plus verification type/version, make state transitions atomic, and pass a stable idempotency key to providers that support one.

**Acceptance criteria:** Concurrent duplicates result in one external operation and one terminal transition; restart and redelivery do not repeat completed verification; unknown provider outcomes enter a reconcilable state.

**Owner:** Sellers with the integration-provider owner.

### TD-BCPS-005 — Publish and probe owned worker health endpoints from AppHost (P1)

**Risk:** Basket EventsProcessor, Customers Messaging, Sellers Service, and Sellers EventsProcessor expose `/health`, but their AppHost resources do not publish or probe those endpoints. AppHost can treat a running process as available while its critical storage or broker dependency is unavailable.

**Work:** Give each worker a unique AppHost HTTP endpoint and attach `WithHttpHealthCheck("/health")` without routing probes through an API or Gateway.

**Acceptance criteria:** Every worker has a unique reachable health URL; critical dependency loss changes `/health` while `/alive` remains successful; AppHost gates dependents on readiness and recovers without restarting healthy resources.

**Owner:** Shared platform/AppHost with Basket, Customers, and Sellers for runtime assertions.

### TD-BCPS-006 — Certify production third-party adapters (P1)

**Risk:** Sellers document-intelligence and tax-authority integrations and Products content-safety integration do not yet prove production authentication, timeout, rate-limit, data-residency, and failure-classification behavior. These optional providers must not accidentally become readiness dependencies.

**Work:** Complete provider-specific production adapters with bounded timeouts, redacted telemetry, circuit breaking, explicit transient/permanent mapping, secret-store integration, and operational runbooks.

**Acceptance criteria:** Provider sandbox contract tests pass; secrets never enter source, logs, traces, or health output; outages follow a documented degraded business path; optional-provider failure does not make the process unready unless product requirements explicitly reclassify it as critical.

**Owner:** Products and Sellers with platform security and provider-integration owners.

### TD-BCPS-007 — Complete full-topology readiness and recovery validation (P1)

**Risk:** Full-topology startup is proven on Windows with Podman 5.8.3, and unit and in-process tests cover endpoint contracts. Real Redis, MongoDB, blob, and broker failure/recovery behavior remains unproven for all nine executables.

**Work:** Run the complete topology in a healthy container environment and interrupt each critical dependency independently. Measure probe and recovery timing and verify correlation across retry/redelivery.

**Acceptance criteria:** All nine executables reach healthy; each critical dependency failure returns `/health` 503 while `/alive` stays 200; health recovers without process restart within the documented objective; responses and telemetry remain non-sensitive.

**Owner:** Shared AppHost/platform for the environment; Basket, Customers, Products, and Sellers for assertions.

### TD-BCPS-008 — Normalize .NET SDK provisioning (P2)

**Risk:** The machine-wide .NET 10 SDK used during hardening was incomplete, requiring an isolated official SDK. Developers and CI can fail before compilation or silently build with inconsistent tooling.

**Work:** Repair managed SDK provisioning and pin the supported version through the repository's approved shared tooling process.

**Acceptance criteria:** Clean developer and CI environments restore, build every owned project, and pass all owned tests with the same supported SDK version.

**Owner:** Developer-experience/CI platform owner.

### TD-BCPS-009 — Remove shared domain-package build warnings (P2)

**Risk:** Rebased owned-service builds still emit nullable-initialization warnings from Basket, Customers, and Products domain models and member-hiding warnings from shared product and seller events. Persistent baseline warnings can conceal new regressions and make a warnings-as-errors quality gate impractical.

**Work:** Correct nullability contracts in the affected domain entities and DTOs, and make inherited event-member behavior explicit with the appropriate override, rename, or intentional `new` declaration. Treat public package-contract changes as versioned shared-domain work.

**Acceptance criteria:** Basket, Customers, Products, and Sellers dependency graphs build without nullable or member-hiding compiler warnings; serialized contracts remain compatible or are intentionally versioned; domain contract tests cover required versus optional fields and inherited event metadata.

**Owner:** Basket, Customers, Products, and shared domain-contract owners. The bounded-service hardening branch must not change `src/Domain` to close this item.
## Orders, Orchestration, Payments, and PSP follow-up

The following items remain from the Orders, Orchestration, Payments, and PSP production-readiness pass completed on 2026-07-20. The implemented baseline includes `/alive` and `/health`, shared OpenTelemetry defaults, structured logging, deterministic message correlation, saga transactional outbox behavior, Orders consumer inbox/outbox support, bounded health checks, payment configuration validation, and duplicate suppression within the currently available persistence boundaries.

| ID | Priority | Area | Risk and required follow-up | Acceptance criteria |
|---|---|---|---|---|
| OSD-01 | P0 | Payments API and Payments.Messaging | Duplicate suppression is process-local because Payments has no durable store. Provision an owned database and transactional idempotency ledger keyed by operation type and stable payment/provider keys; apply a database-backed MassTransit inbox/outbox. | Duplicate payment and refund requests remain single-effect across restart, concurrent delivery, redelivery, and broker reconnect; state and outgoing events commit atomically. |
| OSD-02 | P0 | Refund processing | `RefundPaymentCommandConsumer` publishes `PaymentRefundedEvent` without executing or persisting a provider refund. Implement a durable requested/pending/succeeded/permanently-failed provider workflow. | Publish `PaymentRefundedEvent` only after provider-confirmed success; reconcile uncertain outcomes and represent permanent rejection distinctly. |
| OSD-03 | P0 | PSP/payment configuration | AppHost does not supply PSP.Simulator with `Payment__SignatureSecret`, and the existing development value is shorter than validation requires. Move a secret of at least 32 characters to the platform secret mechanism and define rotation. | Payments.API, Payments.gRPC, and PSP.Simulator start through AppHost with no literal secret in tracked source or generated manifests; rotation never permits unsigned callbacks. |
| OSD-04 | P1 | Orders.API | Order persistence and submission-event publication still form a dual-write boundary. Add a transactional bus outbox or durable dispatch record to order placement. | A committed order eventually produces one logical submission after crash/restart; a rolled-back order produces none. |
| OSD-05 | P1 | Payment initiation | The real PSP idempotency scope and ambiguous-timeout behavior are unproven. Verify the provider contract, persist attempts, and reconcile provider status before retrying an uncertain operation. | Timeout after provider acceptance cannot cause a second charge; reconciliation reaches a durable terminal or operator-action state. |
| OSD-06 | P1 | Payments.gRPC readiness | PSP readiness policy needs an explicit workload decision. Keep third-party failure isolated from unrelated payment endpoints and workers while deciding whether initiation itself should reject traffic. | Fault injection proves only the process/workload that requires live PSP access becomes unready, with bounded sanitized checks. |
| OSD-07 | P1 | Worker deployment | Orders.Messaging, Orchestration, and Payments.Messaging use management ports 8004, 8005, and 8007, but AppHost and deployment probes do not declare them. Add private endpoints and probes in shared deployment configuration. | Every deployed worker has collision-free private `/alive` and `/health` probes; management ports are not public. |
| OSD-08 | P1 | Saga system verification | Full-topology startup is proven with Podman 5.8.3, but the five PostgreSQL/RabbitMQ system tests remain unverified. Run them with `RUN_ORDER_SAGA_SYSTEM_TESTS=true` on the supported runtime; Podman 6.0.x on Windows is excluded until its forwarding regression is resolved. | Restart recovery, `xmin` concurrency, timeout races, duplicate replay, and persisted outbox dispatch tests pass in CI and the supported developer environment. |
| OSD-09 | P2 | PSP simulator | Payment and resolution state is in memory, so restart loses pending operations and duplicate history. Document deliberate ephemeral reset semantics or add lightweight durable test storage. | Restart behavior is deterministic and covered; duplicate initiation after restart cannot silently invalidate system-test outcomes. |
| OSD-10 | P2 | Observability export | Application Insights ingestion and production dashboards still depend on shared OTLP/Azure Monitor configuration. Configure export, resource attributes, sampling, alerts, and bounded dimensions. | Telemetry from all seven executables is visible with trace/order correlation; exporter failure does not affect readiness. |
| OSD-11 | P2 | Messaging metrics | Framework retry/fault metrics are not consistently captured by a common MassTransit observer. Add approved shared receive/retry instrumentation without payload-derived dimensions. | Forced failures record bounded endpoint/message/outcome categories once per attempt or fault without identifiers, payloads, or exception text in tags. |
| OSD-12 | P2 | Shared web defaults | Swagger, CORS, ProblemDetails, proxy handling, sanitized health responses, and timeout budgets remain partly process-local. Consolidate them through a separately reviewed `ServiceDefaults` change. | Architecture tests prevent permissive CORS, detailed exceptions, untrusted forwarded headers, or inconsistent management-listener policy. |

OSD-01 through OSD-05 must be resolved before enabling real-money payment or refund processing. OSD-08 must be resolved before treating the saga reliability work as fully release-verified. Shared-platform items are requests only and must not be implemented from an owned bounded-context change.
