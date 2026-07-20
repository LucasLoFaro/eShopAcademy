# Observability

Status: current-state inventory and proposed production standard. Application Insights export, redaction, dashboards, and alerts are not implemented by this repository today.

## Current inventory

All 28 executable `Program.cs` files identified in `health-endpoints.md` call `AddServiceDefaults`. Unless `Aspire:Monitoring:Enabled` is false, `src/ServiceDefaults/Extensions.OpenTelemetry.cs` registers:

- OpenTelemetry logging with formatted messages and scopes;
- metrics for ASP.NET Core, `HttpClient`, and the .NET runtime;
- traces for the application-name `ActivitySource`, ASP.NET Core, gRPC clients, `HttpClient`, and the `MassTransit` source;
- OTLP export only when `OTEL_EXPORTER_OTLP_ENDPOINT` is non-empty.

The host's normal logging providers remain active. `Operations.Service/Program.cs` and `Orchestration/Program.cs` additionally call `AddConsole`; no central structured logging policy, event IDs, redaction processor, sampling policy, or log volume limits are configured. If no OTLP endpoint is supplied, OpenTelemetry signals have no configured production exporter.

| Area | Current state | Affected files / evidence |
|---|---|---|
| OpenTelemetry registration | Central and broadly adopted, but controlled by a combined monitoring flag | `ServiceDefaults/Extensions.cs`, `Extensions.OpenTelemetry.cs`, every executable `Program.cs` |
| Logging | Default host logging plus OTel logging; formatted messages and scopes exported; two redundant console registrations | `Extensions.OpenTelemetry.cs`, `Operations.Service/Program.cs`, `Orchestration/Program.cs`, sparse `appsettings.json` logging sections |
| `Console.WriteLine` | Production paths contain 19 calls: 13 saga transitions, 5 Basket cache/product messages, and one payment webhook message | `Orchestration/Saga/OrderStateMachine.cs`, `Basket/Data/BasketCache.cs`, `Basket/Data/ProductCache.cs`, `Payments/Payments.API/Program.cs` |
| HTTP server/client | ASP.NET Core and `HttpClient` metrics/traces are enabled; bodies are not intentionally captured | `Extensions.OpenTelemetry.cs`; all APIs and typed/named clients |
| gRPC | gRPC **client** tracing package/registration is enabled; server requests rely on ASP.NET Core instrumentation; no gRPC metrics/interceptor standard | `ServiceDefaults.csproj`, `Extensions.OpenTelemetry.cs`, Orders/Payments/Stock/Products/Shipping gRPC code |
| Database/cache | No EF Core/Npgsql, MongoDB, or Redis instrumentation/metrics package or custom activity is registered | Orders/Orchestration EF, all Mongo contexts/repositories, Basket and Shipping.Simulator Redis |
| MassTransit | `AddSource("MassTransit")` captures MassTransit diagnostic activities and normal W3C propagation is expected; no propagation/retry correlation test or messaging meters are present | `Extensions.OpenTelemetry.cs`, `Extensions.MassTransit.cs`, transport configurators |
| Export | Conditional generic OTLP only; no Application Insights connection/export registration, collector policy, or failure metric | `Extensions.OpenTelemetry.cs`, `ServiceDefaults.csproj` |
| Resource attributes | No explicit `service.name`, namespace, version, environment, instance, region, deployment ring, or cloud resource configuration in code. `AddSource` uses application name but is not resource configuration | `Extensions.OpenTelemetry.cs`; `BaseMessage.cs` has separate message metadata |
| Sensitive-data risk | EF sensitive-data logging is enabled for saga DB. Logs include notification email, shipping destination/email/tracking, seller tax ID/document URL, and Content Safety image URL. Formatted-message export can duplicate rendered sensitive values | `Orchestration/Program.cs`, Notifications/Sellers/Shipping consumers, `ScheduleShippingCommandConsumer.cs`, `ContentModerationService.cs`, `Extensions.OpenTelemetry.cs` |

## Target Application Insights / OpenTelemetry architecture

1. Applications use the OpenTelemetry APIs/SDK through ServiceDefaults. Instrumentation and resource conventions remain vendor-neutral.
2. Production exports OTLP over TLS to one managed OpenTelemetry Collector/agent per environment. The collector performs batching, memory limiting, attribute allow-listing/redaction, tail sampling, and exports traces/logs/metrics to Azure Monitor Application Insights. A direct Azure Monitor OpenTelemetry distribution/exporter is an allowed fallback when no collector is operated, but an app must never enable both paths.
3. `APPLICATIONINSIGHTS_CONNECTION_STRING` or collector credentials are secret configuration, supplied by managed platform references/Key Vault. They are validated for presence only when that export mode is selected and are never logged or returned by health checks.
4. Every signal carries low-cardinality resources: `service.name` (stable AppHost/deployment name), `service.namespace=eshopacademy`, `service.version`, `deployment.environment.name`, `service.instance.id`, cloud provider/region/resource ID, and deployment ring. Resource values must not contain customer or order data.
5. W3C trace context flows HTTP → gRPC → MassTransit → worker/provider HTTP. `CorrelationId`/`EventId` are structured log fields and span attributes under an approved identifier policy, not resource attributes or metric dimensions.
6. Sampling is parent-based. The collector retains 100% of errors, retries, dead letters, and P0 payment/refund/stock/shipping/order-compensation traces; normal successful traffic is tail-sampled to a configurable rate. Metrics are never sampled. Logs below Information are disabled in production except targeted, time-bounded overrides.
7. Telemetry export is fail-open and bounded: exporter outage never changes readiness or blocks business requests beyond the batch/export timeout. Queue drops and export failures produce local self-diagnostic counters/alerts.

### Span and log conventions

- Name server spans by route, not raw URL. Name messaging spans by operation and endpoint, not message ID. Name database spans by system/operation/collection or normalized statement; never capture full SQL, Mongo documents, Redis keys containing user IDs, request/response bodies, message bodies, headers, query tokens, or email/address/tax data.
- Structured logs use stable event names/IDs, severity, `trace_id`, `span_id`, service, endpoint/operation, outcome, duration, attempt, and approved opaque IDs. Exceptions are recorded once at the handling boundary.
- Replace console writes with `ILogger`/saga activity events. Do not log “success” before the durable state/outbox commits.
- Query-string `access_token` used for Gateway SSE must be removed from URL, dependency, and request telemetry at ingestion and never included in scopes.

## Baseline metrics

All histograms publish explicit, consistent units and deployment-appropriate buckets. Dimensions are bounded to service, route template/RPC method, endpoint, message type, dependency type/name, result class, and error category. IDs, email, address, document URL, reason text, exception message, and provider transaction IDs are forbidden dimensions.

| Domain | Required metrics |
|---|---|
| HTTP/gRPC | request count, active requests, duration, response/status class, rejected/rate-limited count, request/response size where safe |
| Dependencies | call count, duration, timeout, circuit state, retry count, result class for HTTP/gRPC, PostgreSQL, MongoDB, Redis, Blob, and external providers |
| Runtime | process CPU, working set, GC collections/pause/allocation, thread pool queue/threads, exceptions, container restart/uptime |
| MassTransit/broker | sent/published/consumed/faulted counts, consume duration, in-flight, immediate retries, redeliveries, skipped/error/dead-letter arrivals, queue depth, oldest message age, consumer availability |
| Inbox/outbox | duplicate suppressed, inbox processing age, outbox pending count/oldest age, dispatch success/failure, reconciliation/unknown outcome count |
| Orders/saga | orders submitted; state transition count/duration; saga age; timeout scheduled/fired/unscheduled; compensation count/outcome; optimistic conflict count |
| Payments/refunds | initiated/completed/failed/refund-requested/refunded; provider duration/result; unknown outcome/reconciliation; amount only as an aggregated measure with controlled currency dimension |
| Stock | reservation created/committed/released/failed; duplicate rejected; inventory adjustment; reservation age/expiry |
| Shipping | scheduled/pickup-confirmed/shipped/delivered/failed/cancelled; provider result/duration; duplicate provider request suppressed |
| Notifications/sellers | notification queued/sent/failed, provider result, seller verification result, sale-ledger registration/duplicate suppression |

Minimum alerts: availability/SLO burn for public APIs; sustained 5xx/gRPC failures; dependency timeout/circuit open; any P0 error/dead-letter; retry/redelivery surge; queue oldest age; outbox backlog; payment unknown outcome; stock duplicate/negative invariant; saga stuck beyond expected duration; and telemetry drop/export failure.

## Findings and acceptance

| ID | Severity | Affected files | Finding and proposed standard | Acceptance criteria | Recommended automated test |
|---|---|---|---|---|---|
| OBS-01 | High | `src/ServiceDefaults/Extensions.OpenTelemetry.cs`, `ServiceDefaults.csproj`, deployment configuration | Export is generic OTLP-only and optional, with no defined Application Insights path. Implement exactly one production export mode and collector/direct-Azure configuration above. | A production-like test emits one correlated trace/log/metric set visible in a test Application Insights resource; exporter outage does not fail readiness/request. | Collector/export smoke test plus exporter-blackhole resilience test. |
| OBS-02 | High | `Extensions.OpenTelemetry.cs`, all executable projects | Resource identity is implicit and inconsistent with AppHost names. Add the required resource attributes centrally. | Every signal has stable service, namespace, version, environment, and instance; no PII/high-cardinality resources. | In-memory exporter snapshot test executed for every executable assembly. |
| OBS-03 | High | Orders/Orchestration EF files, Mongo context/repositories, Basket/Shipping.Simulator Redis files | Owned data stores have no standard traces/metrics. Add safe EF/Npgsql, Mongo, and Redis instrumentation or thin approved activities without statements/documents/keys. | Dependency spans join the inbound trace, durations/errors are measurable, and payload/key capture is disabled. | Integration tests against each store with an in-memory exporter and sensitive canary assertions. |
| OBS-04 | High | `Extensions.MassTransit.cs`, both transport configurators, all consumers and saga | Trace propagation is assumed and messaging health/retry/dead-letter metrics are absent. Add tested propagation and platform meters. | One trace spans producer, broker send/consume, consumer DB/provider work, retry/redelivery; metrics expose endpoint outcomes without IDs. | RabbitMQ and Azure Service Bus trace-continuity tests and metric snapshots. |
| OBS-05 | Medium | Saga, Basket cache/product, Payments.API console call sites; Operations/Orchestration `AddConsole` | Production uses console writes and redundant provider registration. Replace with structured events and central provider/filter configuration. | No production `Console.Write*`; each transition/failure emits one structured event correlated to a trace. | Architecture test forbidding `Console.Write*` outside tests/simulators and log-event snapshot tests. |
| OBS-06 | Critical | `Orchestration/Program.cs`; Notifications, Sellers, Shipping, Content Moderation logging; Gateway SSE token handling; `Extensions.OpenTelemetry.cs` | Sensitive-data logging and formatted export can disclose PII, tax/document data, tracking/address, tokens, and SQL values. Disable EF sensitive logging and apply allow-list/redaction at SDK and collector. | Secret/PII canaries never appear in exported logs, spans, metric dimensions, health responses, or exporter diagnostics. | End-to-end telemetry canary test scanning all exported attributes/bodies; architecture test forbids `EnableSensitiveDataLogging`. |
| OBS-07 | Medium | ServiceDefaults and all bounded contexts | No shared sampling, semantic naming, domain metric, dashboard, or alert baseline exists. Adopt the conventions and metrics above with owners/runbooks. | Required metrics are emitted; dashboards and actionable alerts have owner, threshold/SLO link, and runbook; cardinality stays within budget. | Metric contract/cardinality tests plus alert-as-code validation in the deployment branch. |

## Ownership split

Shared platform work owns resource attributes, exporter selection, collector contract, redaction, sampling, HTTP/gRPC/runtime/database/messaging instrumentation, logging conventions, common meters, and telemetry contract tests. Bounded-context agents replace local console/sensitive logs, add domain activities/metrics, choose safe error categories, and build dashboards/alerts for their P0 flows. Operations must own collector/Application Insights deployment, retention, access control, budget, and runbooks in a later pipeline/IaC branch.
