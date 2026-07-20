# Architecture overview

Status: current implementation, validated against the solution, AppHost, service registrations, and shared defaults on 2026-07-20.

## Platform

eShopAcademy is a .NET 10 microservices application with domain-oriented projects, independently hosted APIs and workers, and explicit integration contracts. The local distributed application is defined by `src/AppHost/AppHost.csproj` and `src/AppHost/AppHost.cs` using Aspire 13.4.

Aspire remains the local orchestration model. It starts application projects and local dependencies, injects connection strings and service-discovery endpoints, orders startup with `WaitFor`, and exposes local telemetry in the Aspire dashboard.

## Service boundaries

| Area | Hosted processes | Owned local persistence |
| --- | --- | --- |
| Basket | Basket API; basket events processor | Redis |
| Products | Products API; Products gRPC | MongoDB `products` |
| Stock | Stock API; Stock gRPC; stock messaging processor | MongoDB `stock` |
| Orders | Orders API; orders messaging worker | PostgreSQL `orders` |
| Orchestration | MassTransit order state-machine worker | PostgreSQL `orchestration` |
| Payments | Payments API; Payments gRPC; payments messaging worker; PSP simulator | No durable payment store is currently registered |
| Shipping | Shipping API; shipping worker; shipping simulator | MongoDB `shipping`; simulator uses Redis database 1 |
| Customers | Customers API; customers messaging worker | MongoDB `customers` |
| Sellers | Sellers API; seller worker; seller events processor | MongoDB `sellers`; product-image storage is configured separately |
| Operations | Operations API; operations worker | MongoDB `operations` |
| Notifications | Notifications API; notifications worker | MongoDB `notifications` |
| Edge/UI | YARP gateway; consumer Vite frontend; seller Vite microfrontend | None |

The AppHost also creates local PostgreSQL, MongoDB, Redis, RabbitMQ, pgAdmin, and an Azurite resource. The Azurite resource is currently not wired to the `productimages` connection used by Sellers; the current connection comes from AppHost configuration. This is recorded as a gap rather than changed speculatively.

## Communication

- Browser traffic enters through the YARP gateway. The gateway uses Aspire service discovery for the bounded-context APIs.
- Internal request/response calls use HTTP and gRPC where an immediate result is required.
- MassTransit is the application messaging abstraction. Consumers, sagas, and contracts do not select a broker directly.
- Development defaults to RabbitMQ through the Aspire `rabbit` resource.
- Non-development defaults to Azure Service Bus. `Messaging:AzureServiceBus:NamespaceUri` is required and authentication uses `DefaultAzureCredential`.
- The order workflow is coordinated by `Orchestration/Saga/OrderStateMachine.cs`, with Entity Framework persistence and transport-specific scheduling selected in `ServiceDefaults`.

The detailed messaging contract is documented in the [transport decision](../plans/production-readiness/messaging-transports.md) and [topology inventory](../plans/production-readiness/messaging-topology.md).

## Health and observability

All application hosts call `AddServiceDefaults`. Web applications that call `UseDefaultEndpoints` expose `/alive` and `/health`; both currently contain only the process self-check. Worker services do not expose HTTP probe endpoints. Detailed dependency readiness remains planned work.

OpenTelemetry logging, ASP.NET Core/HTTP client/runtime metrics, and ASP.NET Core/gRPC client/HTTP client/MassTransit tracing are configured centrally when `Aspire:Monitoring:Enabled` is true. An OTLP exporter is activated when `OTEL_EXPORTER_OTLP_ENDPOINT` is set. There is no direct Application Insights exporter or checked-in collector/deployment configuration yet; Application Insights is the intended production destination.

## Current versus planned

Implemented today:

- Aspire local orchestration and service discovery.
- RabbitMQ local transport and Azure Service Bus transport selection behind MassTransit.
- PostgreSQL, MongoDB, and Redis local resources.
- Order saga with scheduling, happy-path transitions, and several compensation paths.
- Central OpenTelemetry registration and process-level health endpoints for mapped web hosts.

Not established by this repository today:

- Production Azure infrastructure or deployment pipeline.
- Provisioned Service Bus entities, identities, RBAC, or production configuration.
- Application Insights deployment/export configuration.
- Dependency-aware readiness for APIs or management endpoints for workers.
- Complete retry, inbox/outbox, idempotency, security, secrets, and resilience controls.

See [documentation-validation-gaps.md](../plans/documentation-validation-gaps.md) for actionable work and production-blocking status.

