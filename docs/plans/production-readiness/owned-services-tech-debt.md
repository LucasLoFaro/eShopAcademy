# Owned services production-readiness tech debt

Status: open follow-up work as of 2026-07-20.

Scope: `src/Stock`, `src/Shipping`, `src/Operations`, `src/Notifications`, and `src/Gateway`, including simulators in those directories. This file records work that remains after the owned executables received health endpoints, telemetry, configuration validation, retry conventions, and initial idempotency coverage. It does not authorize changes to shared platform files or other bounded contexts.

## Priority definitions

- **P0**: a production rollout can expose a security, correctness, or availability failure; resolve before rollout.
- **P1**: controls exist, but realistic infrastructure or crash-boundary verification is incomplete; schedule before broad production traffic.
- **P2**: maintainability or assurance gap with a lower immediate operational impact.

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

**Risk:** Project builds and automated tests validate endpoint contracts in isolation, but a full Aspire run has not yet proved endpoint publication, dependency wiring, bounded timeouts, or Gateway readiness isolation across the deployed process graph.

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

