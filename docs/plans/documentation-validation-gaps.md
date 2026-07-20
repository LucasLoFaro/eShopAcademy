# Documentation validation gaps

Status: actionable repository gaps identified by the documentation and implementation audit on 2026-07-20. These are intentionally not implemented as part of the documentation reorganization because they require infrastructure, security, operational, or architecture decisions.

The detailed evidence and acceptance standards in [`production-readiness/`](production-readiness/README.md) should be used when turning these sections into issues or Codex tasks.

## Production Azure platform and delivery pipeline are absent

- **Current state:** The repository has a .NET 10 Aspire AppHost for local orchestration and one GitHub Actions workflow that restores, builds, and tests. No Azure infrastructure, environment definition, publish output, deployment workflow, rollback procedure, or operations runbook is checked in.
- **Expected state:** A repeatable, reviewed production deployment that preserves the Aspire application model and provisions the selected Azure compute, networking, data services, identities, secrets/configuration references, probes, scaling, migration jobs, and rollback controls.
- **Evidence:** `src/AppHost/AppHost.cs` adds local resources only inside `builder.Environment.IsDevelopment()`; `.github/workflows/dotnet-ci.yml` contains build/test only; no `azure.yaml`, Bicep, Terraform, Helm, Kubernetes, or production deployment files exist.
- **Impact:** The application cannot be reproducibly deployed, secured, operated, or recovered in production from repository artifacts.
- **Recommended solution:** Make a deployment-target decision, create infrastructure-as-code and environment parameter contracts, publish/deploy through an approved Aspire-compatible workflow, add pre-deployment validation and migration stages, and document rollback/ownership. Do not substitute Docker Compose for Aspire.
- **Dependencies or decisions required:** Azure hosting target, network topology, data-service SKUs, environment strategy, identity model, domain/TLS ingress, ownership, budgets, and release/rollback policy.
- **Suggested priority:** High.
- **Blocks production readiness:** Yes.
- **Relevant files/projects:** `src/AppHost/AppHost.cs`, `src/AppHost/Setup/**`, `.github/workflows/dotnet-ci.yml`, `src/aspire.config.json`.

## Azure Service Bus infrastructure and production proof are missing

- **Current state:** Application code selects Azure Service Bus outside Development and configures MassTransit with `DefaultAzureCredential`, native scheduling, and optional runtime topology creation. The topology manifest exists, but no namespace, queues/topics/subscriptions, identity, RBAC, or production-like end-to-end test is provisioned.
- **Expected state:** Pre-provisioned, versioned Service Bus entities matching `messaging-topology.json`, least-privilege identities for every host, explicit topology ownership, capacity/retention/dead-letter settings, and cross-transport integration tests.
- **Evidence:** `src/ServiceDefaults/Messaging/AzureServiceBusTransportConfigurator.cs`, `MessagingOptions.cs`, `docs/plans/production-readiness/messaging-topology.json`; no Service Bus IaC or deployment configuration exists.
- **Impact:** The production-default transport cannot be demonstrated to start or preserve routing/scheduling semantics, and `CreateTopology=false` cannot work without external provisioning.
- **Recommended solution:** Provision the manifest through IaC, map each workload identity to minimal send/listen/manage roles, default production to pre-provisioned topology, validate both scheduler paths, and add an isolated Azure Service Bus integration environment.
- **Dependencies or decisions required:** Namespace tier/regions, topology-creation ownership, entity naming/versioning, retention/DLQ policy, disaster recovery, identity boundaries, and test-subscription budget.
- **Suggested priority:** High.
- **Blocks production readiness:** Yes.
- **Relevant files/projects:** `src/ServiceDefaults/Extensions.MassTransit.cs`, `src/ServiceDefaults/Messaging/**`, `src/Orchestration/Orchestration/Program.cs`, `docs/plans/production-readiness/messaging-*.{md,json}`.

## Application Insights production observability path is not implemented

- **Current state:** `ServiceDefaults` registers vendor-neutral OpenTelemetry and conditionally exports OTLP. No Azure Monitor/Application Insights exporter, collector configuration, connection contract, resource standard, redaction, sampling, dashboards, alerts, or runbooks are checked in. Several sensitive logging risks remain.
- **Expected state:** Exactly one documented production path to Application Insights, with stable resource attributes, secure credential/configuration sourcing, PII/secret redaction, bounded fail-open export, sampling policy, domain and dependency telemetry, dashboards, alerts, retention, and runbooks.
- **Evidence:** `src/ServiceDefaults/Extensions.OpenTelemetry.cs` checks only `OTEL_EXPORTER_OTLP_ENDPOINT`; `ServiceDefaults.csproj` has no Azure Monitor exporter; `docs/plans/production-readiness/observability.md` identifies missing instrumentation and sensitive logs.
- **Impact:** Production failures and business-flow stalls would be difficult to detect or diagnose, and current formatted/sensitive logs could disclose protected data if exported without controls.
- **Recommended solution:** Choose collector-to-Azure Monitor or direct Azure Monitor distribution, implement it centrally, add resource/redaction/sampling conventions and telemetry contract tests, then deploy dashboards/alerts with owners and runbooks.
- **Dependencies or decisions required:** Collector ownership, Application Insights workspace topology, data classification/redaction policy, sampling/SLO targets, retention/access control, and cost budget.
- **Suggested priority:** High.
- **Blocks production readiness:** Yes.
- **Relevant files/projects:** `src/ServiceDefaults/Extensions.OpenTelemetry.cs`, `src/ServiceDefaults/ServiceDefaults.csproj`, `src/Orchestration/Orchestration/Program.cs`, notification/shipping/seller consumers, `docs/plans/production-readiness/observability.md`.

## Readiness is process-only and workers have no probe listener

- **Current state:** Health registration is now independent of telemetry, and mapped web hosts expose `/alive` and `/health`. Both routes only evaluate the `self` check. Worker services and several gRPC/simulator hosts do not map management endpoints, and no database/cache/bus checks are registered.
- **Expected state:** Process-only liveness, bounded dependency-aware readiness for owned critical dependencies, sanitized responses, and a private management listener for every worker; external/downstream services and telemetry must not fan out from readiness.
- **Evidence:** `src/ServiceDefaults/Extensions.cs`; the per-process matrix and findings in `docs/plans/production-readiness/health-endpoints.md`.
- **Impact:** An orchestrator cannot distinguish a running but unusable process from a ready instance, safely drain workers, or route around broker/store failures.
- **Recommended solution:** Implement shared bounded check factories and response policy, add private worker management endpoints, register only owned critical dependencies per the matrix, and add outage/shutdown contract tests.
- **Dependencies or decisions required:** Deployment platform probe ports, dependency criticality owners, timeout/budget standard, MassTransit health integration, and response exposure policy.
- **Suggested priority:** High.
- **Blocks production readiness:** Yes.
- **Relevant files/projects:** `src/ServiceDefaults/Extensions.cs`, all API/gRPC/worker `Program.cs` files, `docs/plans/production-readiness/health-endpoints.md`.

## Critical message effects lack retry, inbox/outbox, and idempotency guarantees

- **Current state:** Shared MassTransit registration selects transports and endpoints but configures no retry, delayed redelivery, inbox, or transactional outbox. Critical consumers mutate stores, publish follow-up messages, send email, or call provider APIs without a common durable effect boundary. Several duplicate-delivery failure modes are documented.
- **Expected state:** Classified bounded retries/redelivery, deterministic effect IDs, durable inbox/outbox where state and messages must commit together, provider idempotency/reconciliation, observable error/dead-letter handling, and replay procedures on both transports.
- **Evidence:** `src/ServiceDefaults/Extensions.MassTransit.cs`, both transport configurators, order saga and consumers; consumer-by-consumer evidence in `docs/plans/production-readiness/message-delivery-semantics.md`.
- **Impact:** Crashes or broker redelivery can lose compensation messages, duplicate refunds/shipments/emails/ledger effects, inflate released stock, or leave sagas inconsistent.
- **Recommended solution:** Implement shared policies and supported EF/Mongo/Redis delivery primitives, then migrate P0 workflows in Payments, Orders/Orchestration, Stock, and Shipping with crash/replay tests before lower-risk consumers.
- **Dependencies or decisions required:** Error taxonomy and budgets, inbox/outbox storage/retention, provider idempotency contracts, replay authorization/audit, topology/DLQ policy, and per-domain business keys.
- **Suggested priority:** High.
- **Blocks production readiness:** Yes.
- **Relevant files/projects:** `src/ServiceDefaults/Messaging/**`, `src/Orchestration/**`, `src/Payments/**`, `src/Orders/**`, `src/Stock/**`, `src/Shipping/**`, `docs/plans/production-readiness/message-delivery-semantics.md`.

## Secrets, webhook verification, trust boundaries, and unsafe HTTP retries need design work

- **Current state:** Development AppHost wiring contains literal webhook secrets and fixed tenant/client identifiers; payment signature verification accepts a literal development value; CORS is broad; sensitive services can be reached directly without a uniform service-auth boundary; standard HTTP resilience can retry unsafe methods; production option validation and secret sourcing are incomplete.
- **Expected state:** Managed identity and Key Vault/App Configuration contracts, typed fail-fast option validation, real signed/replay-protected webhooks, exact CORS, trusted forwarded-header/TLS policy, service-level authorization, safe method-aware retries, cancellation/timeout propagation, private blob access, and secret/PII-safe diagnostics.
- **Evidence:** `src/AppHost/Setup/Extensions/**`, `src/Payments/Payments.Infrastructure/Helpers/SignatureHelper.cs`, Gateway and Sellers CORS/auth code, `src/ServiceDefaults/Extensions.cs`; detailed findings SEC-01 through SEC-12 in `security-and-configuration.md`.
- **Impact:** Current defaults could permit forged callbacks, duplicate non-idempotent provider effects, direct authorization bypass, credential leakage, or PII disclosure in a production deployment.
- **Recommended solution:** Treat the security specification as a staged program: first webhook verification and safe HTTP defaults, then option/secret validation and service identity, followed by ingress/CORS/blob/log hardening and automated adversarial tests.
- **Dependencies or decisions required:** Threat model, identity/scopes/roles, ingress and TLS termination, webhook algorithms/key rotation/replay store, secret owners/rotation, blob access model, and data classification policy.
- **Suggested priority:** High.
- **Blocks production readiness:** Yes.
- **Relevant files/projects:** `src/AppHost/Setup/**`, `src/ServiceDefaults/**`, `src/Gateway/**`, `src/Payments/**`, `src/Shipping/**`, `src/Sellers/**`, `docs/plans/production-readiness/security-and-configuration.md`.

## Runtime migrations and automatic seed side effects need a deployment contract

- **Current state:** Orders API and Orchestration run EF migrations during application startup. Development startup seeding exists for Customers, Products, and Stock, and some seeding publishes messages. There is no separate production migration job or centrally enforced production seeding guardrail.
- **Expected state:** Single-writer, pre-rollout migrations under a dedicated identity; read-only schema compatibility checks at runtime; explicit development/test seed commands; production configuration that cannot select seed behavior.
- **Evidence:** `src/Orders/Presentation/API/Program.cs`, `src/Orchestration/Orchestration/Program.cs`, customer/product/stock startup code and seed classes; SEC-09 in `security-and-configuration.md`.
- **Impact:** Multiple replicas can race migrations, runtime identities require excessive privilege, rollout/rollback is coupled to application start, and accidental seed events can mutate production workflows.
- **Recommended solution:** Move migrations into the future deployment pipeline, limit runtime DB permissions, make seeding explicit, and test concurrent startup plus production configuration exclusions.
- **Dependencies or decisions required:** Deployment target/job model, migration ownership and rollback policy, database identity/RBAC, seed-data ownership, and schema compatibility policy.
- **Suggested priority:** High.
- **Blocks production readiness:** Yes.
- **Relevant files/projects:** `src/Orders/Presentation/API/Program.cs`, `src/Orchestration/Orchestration/Program.cs`, `src/Customers/**`, `src/Products/**`, `src/Stock/**`.

## Repository membership and duplicate frontend need an ownership decision

- **Current state:** Four project files are outside `src/eShopAcademy.sln`: `Orders/Infrastructure/Messaging/Orders.Messaging.csproj`, `Shipping/Shipping.gRPC/Shipping.gRPC.csproj`, `Stock/Application/Application.csproj`, and `Stock/Tests/StockGrpcClient/Stock.Tests.csproj`. Shipping gRPC is also absent from the AppHost. Independent builds pass for the latter three projects; `Orders.Messaging.csproj` fails with two `CS0535` errors because `OrderMessagingClient` does not implement the current `IOrderMessagingClient.PublishOrderSubmitted` and `PublishCustomerAddressUpdated` contracts. A second seller frontend exists at `src/Sellers/Frontend`, while the AppHost uses `src/Frontend/eshop-sellers`. The standalone Stock gRPC client's invalid project dependencies, package mismatch, namespace mismatch, nonexistent RPC call, and stale endpoint were corrected, but it remains outside normal build/test coverage.
- **Expected state:** Each artifact has explicit ownership and status: active and included in solution/AppHost/CI, intentionally standalone with its own validation, or archived/removed after preserving relevant history.
- **Evidence:** `dotnet sln src/eShopAcademy.sln list`, independent Release builds of all four omitted projects, `src/Orders/Infrastructure/Messaging/OrderMessagingClient.cs`, `src/AppHost/AppHost.csproj`, both seller `package.json` files, and the four project paths above.
- **Impact:** Stale code already fails outside normal CI, other omitted projects can drift unnoticed, and developers may edit the wrong seller frontend or assume Shipping gRPC is locally available.
- **Recommended solution:** Review usages and intended product surface, choose one seller frontend, and either include active projects in solution/AppHost/CI or explicitly archive/decommission them in a separate focused change.
- **Dependencies or decisions required:** Service ownership, Shipping gRPC consumers, intended Stock application/client role, package-library release strategy, and seller UI product direction.
- **Suggested priority:** Medium.
- **Blocks production readiness:** No by itself; active-but-unvalidated artifacts must be resolved before they are included in a production scope.
- **Relevant files/projects:** `src/eShopAcademy.sln`, `src/AppHost/AppHost.csproj`, the four project files above, `src/Frontend/eshop-sellers/**`, `src/Sellers/Frontend/**`.

## Local storage wiring and production image storage are inconsistent

- **Current state:** The AppHost creates an Azurite `storage` resource but does not reference it from an application. Sellers receives `productimages` from a separate connection string whose checked-in value is an Azure Blob service URL. Sellers' upload path creates/uses public blob access according to the security audit.
- **Expected state:** A deliberate local emulator connection for development and a private, identity-based production blob design with upload validation, controlled access URLs, scanning/quarantine, and explicit lifecycle/retention.
- **Evidence:** `src/AppHost/Setup/EnvironmentSetup.cs`, `src/AppHost/appsettings.json`, `src/AppHost/Setup/Extensions/SellersExtensions.cs`, Sellers/Products blob client code, SEC-11 in `security-and-configuration.md`.
- **Impact:** The local emulator consumes resources without validating the actual image path, development can accidentally depend on an Azure endpoint, and the current production concept exposes unsafe public-upload behavior.
- **Recommended solution:** Decide which bounded context owns image storage, wire Azurite through Aspire for Development, use managed identity/private containers in production, and add upload/access security tests. Avoid changing this wiring until ownership and compatibility are decided.
- **Dependencies or decisions required:** Storage owner, API contract and URL model, emulator behavior, managed identity/RBAC, content validation/scanning provider, and migration of existing public URLs.
- **Suggested priority:** Medium.
- **Blocks production readiness:** Yes for seller/product image upload; no for workflows that do not use image storage.
- **Relevant files/projects:** `src/AppHost/Setup/EnvironmentSetup.cs`, `src/AppHost/appsettings.json`, `src/Sellers/Sellers.Api/**`, `src/Products/Infrastructure/Services/BlobStorageClient.cs`.

## End-to-end and production-like validation is incomplete

- **Current state:** Unit and architecture tests cover many consumers, saga transitions, package alignment, transport composition, and topology consistency. CI does not start Aspire, exercise RabbitMQ workflows, use Azure Service Bus, build/lint both active frontends, validate documentation links, or run authenticated browser scenarios.
- **Expected state:** Layered CI with fast solution/frontend checks, Aspire-backed RabbitMQ integration smoke tests, a controlled Azure Service Bus validation stage, and production-like security/health/telemetry/deployment tests. Runtime tests must use discovered Aspire endpoints and isolated data.
- **Evidence:** `.github/workflows/dotnet-ci.yml`, frontend `package.json` scripts, `docs/plans/production-readiness/messaging-regression.md`, and the absence of deployment/test environments.
- **Impact:** Cross-service configuration, browser contracts, local orchestration, and transport parity can regress while project-level tests remain green.
- **Recommended solution:** Add stages incrementally after infrastructure decisions: frontend lint/build, isolated Aspire smoke tests, RabbitMQ workflow regression, then gated Azure Service Bus/Application Insights/deployment validation. Keep credentials and mutable tests isolated from production.
- **Dependencies or decisions required:** CI container support, test identities/secrets, ephemeral data services, Entra test strategy, Service Bus/Application Insights test resources, test-data cleanup, and runtime budget.
- **Suggested priority:** Medium.
- **Blocks production readiness:** Yes for the production-like stages; not all checks need to block local development.
- **Relevant files/projects:** `.github/workflows/dotnet-ci.yml`, `src/AppHost/**`, all test projects, `src/Frontend/eshop-web/**`, `src/Frontend/eshop-sellers/**`, production-readiness specifications.
