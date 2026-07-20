# Shared platform foundation migration guide

Status: implemented reusable foundation with pilot adoption in `Stock.API` and
`Stock.Messaging.Processor`. This is not a service-by-service rollout.

The delivery contract is **at least once**. Retry, inbox/outbox, correlation and
idempotency controls reduce duplicate effects; they do not provide or claim
exactly-once delivery.

## Public extension APIs

| API | Purpose |
|---|---|
| `AddServiceDefaults()` | Base configuration, health registration, optional telemetry, service discovery and safe HTTP defaults. Health remains registered when telemetry is disabled. |
| `AddWebServiceDefaults()` / `UseWebServiceDefaults()` | Restrictive CORS, production ProblemDetails, trusted forwarded headers, correlation logging and security headers. |
| `UseDefaultEndpoints()` / `MapPlatformHealthEndpoints()` | Sanitized `GET /alive` and `GET /health` endpoints. |
| `AddCriticalDependency(name, check, timeout)` | Composes an owned readiness check without moving service-specific dependency code into ServiceDefaults. Default timeout is one second. |
| `AddWorkerHealthEndpoints()` | Adds the lightweight management-only HTTP listener for a Generic Host worker. The default listener is `http://0.0.0.0:8081`. |
| `AddRequiredConnectionString(name)` | Registers a named, validated connection string; startup errors name the missing key without printing its value. |
| `AddValidatedOptions<TBuilder,TOptions>()` | Binds and validates a service-owned typed option contract on startup. Validation messages must name keys, never values. |
| `AddSafeHttpResilience()` | Bounded total/attempt/connect timeouts and bounded retries for safe methods only. No circuit breaker is installed. |
| `AddIdempotentHttpResilience()` | Explicit opt-in for retrying unsafe methods; every unsafe request must carry an `Idempotency-Key` backed by durable service logic. |
| `WithMassTransit()` | Registers transport-neutral bus health and bounded host/consumer shutdown options. |
| `UseReliabilityConventions()` | Applies classified immediate retry and delayed redelivery to receive endpoints. Unclassified, permanent and business failures are not retried. |
| `UseCorrelationId<TMessage>()` | Declares the stable contract-property correlation convention when MassTransit's conventional `CorrelationId` property is not sufficient. |
| `AddEntityFrameworkInboxOutbox<TDbContext>()` | Registers the durable MassTransit EF inbox/outbox. The service must also add the MassTransit outbox entities to its model and migrations. |
| `UseEntityFrameworkInboxOutbox<TDbContext>()` | Enables the registered EF inbox/outbox on an explicitly configured receive endpoint. |

Public reliability types are `MessagingReliabilityOptions`,
`MessageFailureCategory`, `MessageFailureClassifier`,
`TransientMessageException`, `OrderingNotReadyException`,
`BusinessMessageException`, and `PermanentMessageException`.

Public observability types are `TelemetryOptions`, `TelemetryExportMode`, and
`TelemetrySanitizer`. Public health/configuration types are `HealthCheckTags`,
`HealthCheckDefaults`, `WorkerHealthEndpointOptions`,
`RequiredConnectionString`, `HttpResilienceOptions`, and `WebSecurityOptions`.

## Web API migration

1. Replace `AddServiceDefaults()` with `AddWebServiceDefaults()`.
2. Add service-owned typed configuration contracts. Use
   `AddRequiredConnectionString("name")` for every required connection string.
3. Register only owned critical dependencies with `AddCriticalDependency`.
   Do not call downstream HTTP/gRPC services, identity providers or telemetry
   exporters from readiness.
4. Call `UseWebServiceDefaults()` before authorization/endpoints, retain
   `UseDefaultEndpoints()`, and keep Swagger registration optional. The shared
   mapper serves Swagger only in Development.
5. Configure exact CORS origins/methods/headers in `WebSecurity`. Leave origins
   empty for internal APIs. If forwarded headers are enabled, supply exact
   trusted proxy IPs.

## Worker migration

1. Keep `AddServiceDefaults()` and add `AddWorkerHealthEndpoints()` after
   messaging registration. Override `Management:Health:Url` when port 8081 is
   not the platform management port.
2. Register the worker's database/cache check with `AddCriticalDependency`.
   MassTransit bus readiness is already registered by `WithMassTransit()`.
3. Opt into `UseReliabilityConventions()` only after classifying consumer
   failures. Throw/wrap transient, ordering, business and permanent failures
   with the public classification types. Unknown failures receive no automatic
   retry and follow MassTransit's error-queue behavior.
4. For EF transactional services, register and migrate the inbox/outbox, then
   enable it on every state-changing endpoint. MongoDB and Redis services must
   implement a context-owned durable inbox/outbox or atomic deduplication design;
   the platform does not pretend an in-memory outbox is durable.
5. Pass `ConsumeContext.CancellationToken` through all store and provider calls.
   Provider timeouts must remain shorter than the configured consumer stop
   timeout.

## Telemetry configuration

`Aspire:Monitoring:Enabled=false` disables telemetry registration only. It does
not disable health registration or endpoints.

`Telemetry:ExportMode` supports `None`, `Console`, `Otlp`, and `AzureMonitor`.
OTLP requires `Telemetry:OtlpEndpoint` or `OTEL_EXPORTER_OTLP_ENDPOINT`.
Azure Monitor requires `Telemetry:ApplicationInsightsConnectionString` or
`APPLICATIONINSIGHTS_CONNECTION_STRING`. When no mode is explicit, configured
OTLP wins, then configured Application Insights; Development otherwise uses
console and non-Development uses no exporter. `Telemetry:SamplingRatio` is a
parent-based trace ratio in `(0,1]`.

Shared instrumentation covers ASP.NET Core, HttpClient, gRPC clients, runtime,
MassTransit tracing/metrics, EF Core and safe activity sources for Npgsql,
MongoDB diagnostic sources and StackExchange.Redis. Message/request bodies and
headers are not enabled. A final processor strips query strings and redacts
authorization, API-key, cookie, connection-string, credential, message-body,
payment/refund/PSP and customer/PII fields before every configured exporter.

## Decisions each service agent must make

- Which owned stores are readiness-critical and what bounded, non-mutating probe
  is correct. Optional providers and downstream services are not readiness.
- The business idempotency key for every unsafe HTTP/provider/message operation,
  how it is persisted, and how timeout-after-accept outcomes are reconciled.
- The exact classification of each exception/result as transient,
  ordering-not-ready, business, permanent or unclassified. Do not broaden the
  shared retry types to catch every exception.
- Whether delayed redelivery is valid for a specific ordering race and whether
  both configured transports support the chosen scheduler/topology.
- The durable inbox/outbox technology, retention window, unique constraints and
  transaction boundary for the owned database. MongoDB/Redis adoption remains
  context-owned work.
- Which identifiers are safe in logs/spans and which domain metrics and bounded
  dimensions are required. Message bodies, payment data and customer PII remain
  forbidden.
- Exact CORS origins, trusted proxy addresses, authorization policies, provider
  timeout, and any justified per-client circuit breaker.
- The management listener URL/port and deployment network policy that keeps
  health endpoints private.

## Pilot result

`Stock.API` now uses the hardened web defaults, validates the `stock` connection
string, reports MongoDB and MassTransit readiness, and keeps `/alive` self-only.
`Stock.Messaging.Processor` now exposes the management health listener, validates
the same connection string, reports MongoDB plus bus readiness, applies the
classified reliability convention, and uses bounded graceful shutdown. No stock
reservation business logic, state transition or idempotency behavior was changed.

The Stock Mongo check lives with `StockDbContext` as `PingAsync`; ServiceDefaults
contains only the composable health registration primitive.
