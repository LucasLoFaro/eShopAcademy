# Security and configuration

Status: current-state risk inventory and proposed production standard. Findings are not claims that the recommended controls are already implemented.

## Current inventory

### Configuration and secret sources

| Category | Current source and consumers | Risk/notes |
|---|---|---|
| MongoDB connection strings | `products`, `stock`, `customers`, `shipping`, `operations`, `notifications`, and `sellers` consumed by their API/worker context or repository constructors | Mostly raw strings; validation is inconsistent and no shared secret-redaction rule exists |
| PostgreSQL connection strings | `orders` in Orders.API/Orders.Messaging; `orchestration` in Orchestration | Missing values are passed to EF; two processes migrate at startup |
| Redis connection strings | `Redis` in Basket API/worker; lowercase `redis` in Shipping.Simulator | Key casing is inconsistent; only simulator explicitly rejects missing value |
| Messaging | RabbitMQ URI from `Messaging:RabbitMq:ConnectionString` or `ConnectionStrings:rabbit`; Azure Service Bus namespace uses `DefaultAzureCredential` | Messaging is the only centrally validated option; a Rabbit URI can contain credentials |
| Blob storage | `productimages` connection reference used by Sellers.Api; AppHost `appsettings.json` contains a public service URL | Sellers creates a public-read container and returns public URLs |
| Application configuration/secrets | Non-development `AddDefaultConfiguration` optionally connects to Azure App Configuration with `DefaultAzureCredential` and resolves Key Vault references | `APPCONFIGURATION` is optional, `KEYVAULT` is set locally but not read directly, and required-key validation is not centralized |
| Provider credentials | SendGrid API key is an Aspire secret parameter; Azure Content Safety uses `DefaultAzureCredential`; future Document Intelligence/tax providers are TODO | SendGrid key/from-address are not validated; default sender is a personal address |
| Authentication configuration | Entra instance/tenant/client/audience are local environment values for Gateway and Sellers.Api | IDs are not secrets but must be environment-specific and validated |
| Webhook signatures | `Payment:SignatureSecret` and `Shipping:SignatureSecret` are hardcoded locally as `Sup3rSecr3t!`; PSP/Shipping simulators send literal `Signature` | Payment verification returns true solely for literal `Signature`; shipping does not validate incoming signatures in the inspected application path |

Production standard: secrets are Key Vault references or managed identities, never source/appsettings/plain environment literals. Non-secret options may use Azure App Configuration. Each process binds typed options and calls `ValidateOnStart`; error messages name missing keys but never values. Connection strings/credentials are prohibited from logs, telemetry, health output, exception text, and API responses. Rotate webhook/provider secrets and support an overlap window identified by key ID.

### CORS, Swagger, transport, and trust boundary

- Gateway and Sellers.Api use `AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()`. Gateway has a configured `SpaOrigin` that is not used by the policy; Sellers has `SellersSpaOrigin` that is not used.
- Swagger is registered in Gateway, Basket, Customers, Notifications, Operations, Orders, Payments, Products, Sellers, Shipping, and Stock APIs. `UseDefaultEndpoints` serves Swagger/UI whenever registered, including production.
- `UseHttpsRedirection` is commented out. No process calls `UseForwardedHeaders`; proxy networks, forwarded proto/host trust, and TLS termination are unspecified. Service discovery explicitly allows only `http`, and Gateway/AppHost destinations are HTTP.
- Gateway authenticates Entra bearer tokens and applies route policies, but several product/stock routes are intentionally unauthenticated. Most bounded-context APIs have no local authentication. Operations seller authorization trusts an `X-Seller-Id` header. Direct service exposure can bypass Gateway policy.

Production standard: external TLS terminates at an approved ingress; forwarded headers are processed before auth/redirect only from known proxies/networks, with a forward limit. Internal cleartext is allowed only on the private workload network under the deployment threat model; otherwise use TLS/mTLS. Services enforce their own authorization for sensitive operations and do not trust an unverified identity header. CORS is disabled for non-browser internal services and exact-origin/method/header policies are configured for browser surfaces. Swagger JSON/UI is development-only or separately authenticated/allow-listed.

### HTTP resilience and cancellation

`AddServiceDefaults` applies `AddStandardResilienceHandler()` to every `HttpClient` without disabling retries for unsafe methods. This affects non-idempotent POSTs in:

- `Payments.gRPC/Services/PaymentService.cs` (`/psp/make-payment`);
- `Shipping.Application/Clients/ShippingProviderClient.cs` (schedule and confirm pickup);
- `Sellers.Api/Program.cs` (create product and increase/decrease stock);
- `PSP.Simulator/Program.cs` and `Shipping.Simulator/Program.cs` (webhooks).

The default must retry only safe/idempotent methods (`GET`, `HEAD`, `OPTIONS`, `TRACE`, and explicitly idempotent `PUT`/`DELETE` by contract). POST/PATCH retries are disabled centrally unless the named client supplies a durable idempotency key and a provider-specific policy. Retry budgets honor `Retry-After`, use jitter, and do not multiply MassTransit redelivery retries.

Cancellation is good in many minimal APIs, gRPC stock/payment calls, Mongo shipping/seller/operations repositories, and consumers. Known gaps are Basket endpoints/cache interfaces, CustomerAddressUpdatedEventConsumer repository calls, Orders `CustomerServiceClient`/`ProductServiceClient`, Products service/repository/moderation and Product gRPC calls, notification email sender and one notification insert, and simulator webhook helpers. `PaymentService` blocks on asynchronous message publishes with `GetAwaiter().GetResult()`. Standard: accept and pass the request/consume/shutdown token through every network/storage/provider call; never replace it with `None`, omit it when an API supports it, or sync-block. Apply a bounded timeout linked to the caller token.

### Startup mutation and sensitive logging

- Orders.API and Orchestration call `Database.Migrate()` during process startup. This creates multi-replica races, unbounded startup time, excess runtime DB privilege, and rollback coupling.
- Development startup seeds Customers, Products, and Stock. Product/Stock seeding publishes messages, which can create significant replay/side-effect traffic.
- Orchestration enables EF `EnableSensitiveDataLogging()`.
- Notifications log recipient email; Shipping logs destination, email, and tracking; Sellers logs tax ID/document URL; Content Moderation logs image URL; SendGrid failure exceptions include the provider response body. `Console.WriteLine` bypasses structured redaction.

Production standard: migrations run as a separately authorized, single-writer deployment job before rollout; applications validate schema compatibility read-only and use least-privilege runtime identities. Seeders are explicit development/test tools, never automatic production startup. Logs use an allow-list of opaque identifiers and categorical outcomes; PII, secrets, addresses, tax/document/provider payloads, and SQL parameter values are excluded or irreversibly tokenized.

## Findings and acceptance

| ID | Severity | Affected files | Finding and proposed standard | Acceptance criteria | Recommended automated test |
|---|---|---|---|---|---|
| SEC-01 | Critical | `Payments.Infrastructure/Helpers/SignatureHelper.cs`; `Payments.API/Program.cs`; PSP simulator; AppHost Payments/Shipping extensions; Shipping simulator | Payment webhook verification is a literal-string bypass and local secrets/signatures are hardcoded. Implement constant-time HMAC over canonical bytes with timestamp/nonce replay protection, key IDs/rotation, and equivalent shipping webhook verification. | Literal `Signature`, stale timestamps, changed bodies, wrong keys, and replayed nonces are rejected; valid current/overlap keys succeed; no secret is in source. | Signature contract/property tests, replay-cache test, and secret-scanner rule for known literals/config keys. |
| SEC-02 | High | `ServiceDefaults/Extensions.Configuration.cs`; AppHost setup/appsettings; all DB/Redis/Messaging registrations; Notification email and Content Safety configuration | Secret/config sourcing and validation are inconsistent. Use managed identity/Key Vault references, typed options, and `ValidateOnStart` for every required dependency/provider/auth option. | Each executable either starts with a valid minimal configuration or fails before binding/listening with a sanitized key-only error; production contains no plaintext secret. | Parameterized startup tests for missing/malformed options plus repository secret scanning. |
| SEC-03 | High | `Gateway/Program.cs`; `Sellers/Sellers.Api/Program.cs`; related AppHost origin environment values | Wildcard CORS ignores configured origins. Use exact environment-specific origins, methods, and headers; reject wildcard credentials combinations and disable CORS where unnecessary. | Only configured origins receive CORS headers; hostile/null origins and unapproved methods/preflights fail. | `WebApplicationFactory` preflight matrix tests for both browser surfaces. |
| SEC-04 | Medium | `ServiceDefaults/Extensions.Swagger.cs`; the ten API `Program.cs` files calling `WithSwagger` | Swagger is exposed whenever registered, including production. Gate it to Development or authenticated/allow-listed operations access. | Production anonymous `/swagger` and `/swagger/v1/swagger.json` return 404/403; development remains usable; schemas do not expose secrets/examples. | Environment-specific endpoint tests and OpenAPI sensitive-schema scan. |
| SEC-05 | High | `ServiceDefaults/Extensions.cs`; Gateway and Sellers programs; Gateway appsettings; AppHost endpoint/reference setup; all web programs | TLS termination and forwarded-header trust are unspecified; internal HTTP and direct-service access can undermine scheme/auth assumptions. Define known proxies/networks, processing order, external HTTPS policy, and service-to-service trust. | Spoofed forwarded headers are ignored; trusted ingress produces correct scheme/host; public HTTP is redirected/rejected by the chosen model; internal services are not publicly reachable. | Forwarded-header spoof/trusted-proxy integration tests and deployment network-policy tests. |
| SEC-06 | High | Gateway route configuration; all bounded-context API `Program.cs`; Operations header-based seller endpoints | Gateway authorization is not a sufficient service boundary, and some sensitive APIs trust direct reachability or `X-Seller-Id`. Enforce local JWT/workload identity and claim-derived seller authorization. | Calling a service directly without a valid user/workload token is denied; a forged seller header cannot grant access; least-privilege scopes/roles are required per route. | Direct-service authorization matrix tests with missing, forged, wrong-tenant, wrong-audience, and wrong-seller tokens. |
| SEC-07 | Critical | `ServiceDefaults/Extensions.cs` and all POST call sites listed above | Global standard resilience can retry unsafe payment/shipping/stock/product/webhook requests. Disable unsafe retries by default and opt in only with durable provider idempotency. | A transient response causes zero automatic POST/PATCH retries without an idempotency policy; opted-in calls reuse the same key and produce one effect. | Fake-handler attempt-count tests for every method/status plus provider duplicate-effect contract tests. |
| SEC-08 | Medium | Basket API/data/interfaces; Customer address consumer/repository calls; Orders customer/product clients; Products services/gRPC; Notification email/consumer; simulators; Payment gRPC service | Cancellation is not consistently propagated and one service sync-blocks async work. Require end-to-end linked cancellation and bounded timeouts. | Client disconnect, consumer shutdown, or timeout cancels downstream work promptly; no `.GetAwaiter().GetResult()` in production async paths. | Architecture analyzer plus cancellation integration tests using hanging fake HTTP/store providers. |
| SEC-09 | High | `Orders/Presentation/API/Program.cs`; `Orchestration/Program.cs`; Customer/Product/Stock startup seed paths | Runtime processes mutate schema/data at startup. Move migrations to a single deployment job and make seeding explicit and environment-guarded. | App runtime identity cannot alter schema; multiple replicas start concurrently without migration races; failed migration blocks rollout before app start; production never seeds. | Least-privilege DB integration test, concurrent startup test, and production-config test forbidding seeder activation. |
| SEC-10 | High | `Orchestration/Program.cs`; Notifications consumers/email sender; Shipping and Sellers consumers; Content Moderation; Basket/Saga/Payments console sites | Sensitive values and provider bodies can be logged/exported. Disable EF sensitive logging and implement allow-list/redaction plus structured logging. | PII/secret canaries never appear in logs, traces, health, error responses, or dead-letter metadata; useful categorical diagnostics remain. | End-to-end log/telemetry canary scan and architecture test forbidding sensitive logging/console writes. |
| SEC-11 | High | `Sellers.Api/Program.cs`; AppHost blob configuration | Upload path creates a public container, trusts MIME/extension, has no explicit size/content/malware policy, and publishes document/image URLs. Use private containers, short-lived scoped access, server-side content validation, size limits, malware scanning/quarantine, and egress restrictions for document analysis. | Anonymous blob read is denied; oversized/polyglot/malicious files are rejected/quarantined; only approved content becomes accessible through controlled URLs. | Azurite/Azure integration tests for ACL/SAS expiry and upload adversarial corpus tests. |
| SEC-12 | Medium | Notification email sender; provider clients; App Configuration/Key Vault clients | Provider SDK/client timeouts, error-body handling, and credential rotation behavior are not standardized. Apply bounded clients, sanitized error categories, managed identity where available, and rotation/reload policy. | Provider failure cannot expose response bodies/secrets or hang beyond budget; rotated credentials take effect by documented restart/reload behavior. | Fake-provider timeout/large-sensitive-error tests and credential-rotation smoke test. |

## Configuration acceptance baseline

Every production process must have a checked-in option contract documenting required/optional keys, secret classification, default policy, owning team, and reload behavior. Automated startup coverage must include all 28 `AddServiceDefaults` executables. A production configuration is accepted only if:

- required options validate before network listeners and consumers start;
- no development default (`guest`, literal signature, personal sender, localhost, public emulator, auto-approval) is reachable;
- managed identities and runtime data roles are least privilege;
- CORS, Swagger, forwarded headers, auth, and service exposure match the deployment trust model;
- unsafe retries are disabled unless idempotency is proven;
- all I/O observes cancellation/timeouts;
- runtime identities cannot migrate schemas; and
- secret/PII canaries are absent from all observable outputs.

## Ownership split

Shared platform work owns typed option conventions/validation, secret-source integration, redaction, safe `HttpClient` defaults, cancellation analyzers, CORS/Swagger/forwarded-header helpers, service-auth primitives, and startup contract tests. Bounded-context agents supply exact option schemas, authorization policies, idempotency keys, provider timeout/retry classifications, log field allow-lists, and data migration/seed ownership. Payment/Shipping agents own webhook cryptography and provider reconciliation; Sellers owns upload/document hardening; deployment agents later own Key Vault/App Configuration references, identities/RBAC, TLS/network policies, and migration jobs.

## Consolidated prioritized implementation plan

This is sequencing guidance for later branches; this specification branch implements none of it.

### Shared platform agent

1. **P0 — Safe defaults and contract tests:** disable unsafe HTTP retries; add typed option/secret validation; define error classification, bounded MassTransit retry/redelivery, correlation headers, sanitized error destinations, and shutdown budget. Add cross-transport and all-executable architecture/startup tests before service adoption.
2. **P0 — Delivery primitives:** provide supported EF and Mongo inbox/outbox registrations, deterministic effect/idempotency interfaces, duplicate metrics, and crash/replay test fixtures. Do not claim exactly-once delivery; guarantee idempotent effects under at-least-once delivery.
3. **P0 — Security boundary:** remove development signature bypass through a reusable webhook verification/replay component; provide service-auth, exact-origin CORS, development-only Swagger, and trusted-forwarded-header policies.
4. **P1 — Health:** decouple health from telemetry, implement sanitized bounded checks, private worker management endpoints, MassTransit/store readiness, and gateway no-fan-out tests.
5. **P1 — Observability:** standardize resources, Application Insights/OTLP export mode, redaction, sampling, messaging/database instrumentation, baseline meters, and telemetry canary tests.
6. **P2 — Operational controls:** cancellation analyzer, migration/startup guardrails, dependency manifest drift tests, dashboard/alert templates, and authenticated dead-letter replay tooling contracts.

### Later bounded-context agents

| Priority | Agent/context | Required service-specific work |
|---|---|---|
| P0 | Stock | Make reservation commit/release atomic and terminal; add released state/conditional updates; deduplicate by reservation; transactionally publish outcomes; prove parallel replay cannot inflate stock or emit false failure. |
| P0 | Payments | Replace signature bypass; persist payment/refund lifecycle; integrate PSP refund with idempotency and unknown-outcome reconciliation; remove sync-over-async; classify provider errors; add webhook/refund crash tests. |
| P0 | Orders + Orchestration | Add Orders and saga EF outboxes/inboxes; generate deterministic effect IDs; make transitions conditional/versioned; prevent duplicate seller-sale requests; move migrations; test duplicate/concurrent saga events and compensation. |
| P0 | Shipping | Persist shipment attempt/provider key and outcome; make schedule/confirm/cancel idempotent; implement cancellation; avoid duplicate enrichment; add webhook verification; test provider timeout-after-accept. |
| P1 | Notifications | Replace swallowed exceptions with durable notification/email outbox status; add uniqueness per order transition/channel; propagate cancellation; sanitize email/provider body logs. |
| P1 | Sellers | Enforce unique sale ledger operation; outbox seller events; harden tax/document verification and provider idempotency; secure blob uploads; replace wildcard CORS and validate identity-to-seller authorization. |
| P1 | Operations | Merge/conditionally update packages instead of destructive replace races; handle missing-package ordering with redelivery; derive seller identity from claims, not an untrusted header. |
| P1 | Basket | Add version/ordering guards for product/stock projections and basket empty/reinstate; stop suppressing Redis failures; propagate cancellation. |
| P1 | Customers | Add address uniqueness/normalized hash and cancellation; distinguish ordering-not-ready from permanent missing customer; avoid concurrent duplicate saved addresses. |
| P1 | Products | Add cancellation and safe Content Safety telemetry; validate Product gRPC dependency graph; keep external moderation out of readiness; make development seeding explicit. |
| P2 | Gateway | Replace wildcard CORS, validate auth/routes at startup, sanitize SSE query tokens, and prove readiness never calls downstream services. |
| P2 | Simulators and remaining gRPC apps | Map management health endpoints, bound/cancel webhook calls, remove literal signatures, and ensure simulator-only behavior cannot be selected in production. |

Exit order is P0 correctness/security before probe or dashboard polish. A context is production-ready only when its acceptance tests in all four specifications pass on both supported message transports where messaging is used.
