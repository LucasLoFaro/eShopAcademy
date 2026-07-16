# Health endpoints

## Contract

- `GET /alive` is process-only liveness. It must not query a database, cache, broker, downstream service, DNS, identity provider, telemetry exporter, or external provider. It returns success once the process has started and failure only for an unrecoverable in-process condition.
- `GET /health` is readiness for this process's critical owned dependencies. It may include the owned database/cache and MassTransit bus when the process cannot serve its core purpose without them. It must not fan out through gateways or query optional/external providers.
- Each dependency probe has a 1-second timeout, no retry, and cancellation. The aggregate request has a 2-second budget. Checks execute independently and must not consume normal request retry policies.
- Responses are unauthenticated only on a private management listener. The body is a fixed `Healthy`, `Degraded`, or `Unhealthy` shape with check names and durations; it never includes connection strings, hosts, database names, exception messages, stack traces, provider bodies, or credentials.
- Telemetry enablement must not control endpoint existence. Disabling `Aspire:Monitoring:Enabled` may disable exporters, but not health registration or mapping.

Current `AddDefaultHealthChecks` registers only `self` tagged `live`. Consequently, every web process that calls `UseDefaultEndpoints()` currently exposes `/alive` and `/health` when monitoring is enabled, but both are effectively process-only. `Host.CreateApplicationBuilder` workers have no HTTP server. “Yes (self)” below means a route exists but readiness is incomplete.

## Dependency matrix

| Process / affected `Program.cs` | Current `/alive`; `/health` | Owned database / critical cache / blob | MassTransit | Other critical dependencies | Must not affect readiness |
|---|---|---|---|---|---|
| Basket.API — `src/Basket/API/Program.cs` | Yes; Yes (self) | Redis is critical | No, despite local AppHost RabbitMQ reference | None | RabbitMQ; downstream product/stock services |
| Basket.EventsProcessor — `src/Basket/EventsProcessor/EventsProcessor/Program.cs` | No; No | Redis is critical | Consumer bus is critical | None | Producers and unrelated APIs |
| Customers.Api — `src/Customers/Customers.Api/Program.cs` | Yes; Yes (self) | MongoDB `customers` is critical | No, despite local AppHost RabbitMQ reference | None | RabbitMQ; seed data status after startup |
| Customers.Messaging — `src/Customers/Customers.Messaging/Program.cs` | No; No | MongoDB `customers` is critical | Consumer bus is critical | None | Customers API |
| Gateway — `src/Gateway/Program.cs` | Yes; Yes (self) | None | No | Reverse-proxy route/auth configuration must validate at startup | **Every downstream**, Entra discovery/JWKS, DNS, and service discovery resolution |
| Notifications.Api — `src/Notifications/Notifications.Api/Program.cs` | Yes; Yes (self) | MongoDB `notifications` is critical | No | None | SendGrid and notification worker |
| Notifications.Service — `src/Notifications/Notifications.Service/Program.cs` | No; No | MongoDB `notifications` is critical | Consumer bus is critical | None | SendGrid provider availability; provider outages are workload failures, not process readiness |
| Operations.Api — `src/Operations/Operations.Api/Program.cs` | Yes; Yes (self) | MongoDB `operations` is critical | Publisher bus is critical for workflow commands/events | None | Shipping/seller/order APIs |
| Operations.Service — `src/Operations/Operations.Service/Program.cs` | No; No | MongoDB `operations` is critical | Consumer bus is critical | None | APIs and external fulfillment systems |
| Orchestration — `src/Orchestration/Orchestration/Program.cs` | No; No | PostgreSQL `orchestration` is critical | Saga bus and scheduler are critical | Database schema compatibility is startup validation | Payment, stock, basket, shipping, and order services; telemetry exporter |
| Orders.Messaging — `src/Orders/Orders.Messaging/Program.cs` | No; No | PostgreSQL `orders` is critical | Consumer bus is critical | None | Downstream compensation consumers |
| Orders.API — `src/Orders/Presentation/API/Program.cs` | Yes; Yes (self) | PostgreSQL `orders` is critical | Publisher/SSE consumer bus is critical | None | Customers/Products HTTP, Payments/Stock gRPC; these fail individual requests and must not create readiness fan-out |
| Payments.API — `src/Payments/Payments.API/Program.cs` | Yes; Yes (self) | None | Publisher bus is critical to accept webhooks | Signature configuration validates at startup | PSP, order service |
| Payments.gRPC — `src/Payments/Payments.gRPC/Program.cs` | Yes; Yes (self) | None | Publisher bus is critical | PSP base URI must be syntactically valid at startup | PSP availability |
| Payments.Messaging — `src/Payments/Payments.Messaging/Program.cs` | No; No | No current owned store | Consumer bus is critical | None today | PSP reference (currently unused); future PSP availability |
| Products.API — `src/Products/Presentation/Products.API/Program.cs` | Yes; Yes (self) | MongoDB `products` is critical | Publisher bus is critical for product events | None | Azure Content Safety and image URLs; development seeding after startup |
| Products.gRPC — `src/Products/Presentation/Products.gRPC/Program.cs` | No; No | AppHost supplies products MongoDB, but Program wires no repository/database | No, despite AppHost RabbitMQ reference | `IProductService` graph must be valid before ready | RabbitMQ; unrelated APIs |
| PSP.Simulator — `src/PSP/PSP.Simulator/Program.cs` | No; No | In-memory process state only | No | None | Webhook destinations |
| Sellers.Api — `src/Sellers/Sellers.Api/Program.cs` | Yes; Yes (self) | MongoDB `sellers` is critical; Blob `productimages` is feature-scoped, not global | Publisher bus is critical for registration workflow | Auth/config validation | Products, Shipping, Orders, Stock APIs; Blob service and Entra availability |
| Sellers.EventsProcessor — `src/Sellers/Sellers.EventsProcessor/Program.cs` | No; No | MongoDB `sellers` is critical | Consumer/publisher bus is critical | None | Other bounded contexts |
| Sellers.Service — `src/Sellers/Sellers.Service/Program.cs` | No; No | MongoDB `sellers` is critical | Consumer/publisher bus is critical | None | Future Document Intelligence/tax providers |
| Shipping.Api — `src/Shipping/Shipping.Api/Program.cs` | Yes; Yes (self) | MongoDB `shipping` is critical | Publisher bus is critical | Provider base URI/config validates at startup | Shipping provider availability |
| Shipping.gRPC — `src/Shipping/Shipping.gRPC/Program.cs` | No; No | None currently wired | No | None | Shipping provider and other services |
| Shipping.Service — `src/Shipping/Shipping.Service/Program.cs` | No; No | MongoDB `shipping` is critical | Consumer/publisher bus is critical | Provider base URI/config validates at startup | Shipping provider availability |
| Shipping.Simulator — `src/Shipping/Shipping.Simulator/Program.cs` | Yes; Yes (self) | Redis database 1 is critical | No | None | Webhook destinations, map/geocoding/tile providers used by browser UI |
| Stock.API — `src/Stock/Stock.API/Program.cs` | Yes; Yes (self) | MongoDB `stock` is critical | Publisher bus is critical | None | Basket/orders services; development seeding |
| Stock.gRPC — `src/Stock/Stock.gRPC/Program.cs` | No; No | MongoDB `stock` is critical | Publisher bus is critical | None | Orders API |
| Stock.Messaging.Processor — `src/Stock/Stock.Messaging.Processor/Program.cs` | No; No | MongoDB `stock` is critical | Consumer/publisher bus is critical | None | Basket/orders services |

No process currently registers MongoDB, PostgreSQL, Redis, Blob Storage, or MassTransit health checks. AppHost `WaitFor` establishes local startup ordering only; it is not runtime readiness.

## Readiness rules by dependency

| Dependency | Readiness probe | Classification |
|---|---|---|
| PostgreSQL | Open a connection and execute a trivial command with the 1-second command timeout; do not migrate | Critical only for the owning API/worker |
| MongoDB | `ping` the configured database/client with server selection and operation bounded to 1 second | Critical only for the owning API/worker |
| Redis | `PING` the already configured multiplexer/database; no key scan | Critical for Basket and Shipping.Simulator |
| MassTransit | Bus health/receive endpoint readiness from MassTransit health checks; do not publish a probe message | Critical when core requests/worker processing require the bus |
| Blob storage | Optional/feature-scoped for Sellers upload; expose a separate dependency metric or degraded detail, not global unready | Non-critical by default |
| HTTP/gRPC downstream | Never call from readiness. Validate required URI/options at startup and use request-time resilience/circuit metrics | Non-critical |
| Entra, SendGrid, PSP, shipping provider, Content Safety, Document Intelligence, tax provider | No readiness call. Invalid required configuration fails startup; runtime outage affects operations, not pod scheduling | Non-critical external |
| OTLP/Application Insights | Never affects liveness or readiness | Non-critical |

## Worker endpoint requirements

Every worker must expose a private management HTTP endpoint even if it has no business HTTP API. The platform registration owns the listener and maps exactly `/alive` and `/health`; it must not add Swagger or application routes. Kubernetes/Container Apps probes target that listener. Readiness remains false until the MassTransit bus and owned store are ready, switches false when the bus/store becomes unavailable beyond the bounded check, and switches false immediately when graceful shutdown begins. Liveness remains true during a recoverable dependency outage.

Required workers are Basket.EventsProcessor, Customers.Messaging, Notifications.Service, Operations.Service, Orchestration, Orders.Messaging, Payments.Messaging, Sellers.EventsProcessor, Sellers.Service, Shipping.Service, and Stock.Messaging.Processor.

## Findings and acceptance

| ID | Severity | Affected files | Finding and proposed standard | Acceptance criteria | Recommended automated test |
|---|---|---|---|---|---|
| HLT-01 | High | `src/ServiceDefaults/Extensions.cs`; every `Program.cs` marked “Yes (self)” | `/health` is only the self check and is coupled to monitoring. Register health independently and add only matrix-critical checks. | `/alive` remains healthy during broker/DB outage; `/health` becomes unhealthy within 2 seconds; disabling exporters does not remove routes. | `WebApplicationFactory` contract tests plus container dependency-stop tests. |
| HLT-02 | High | The eleven worker `Program.cs` files listed above | Workers expose no probe endpoint. Add the platform-owned private management listener and shutdown-aware readiness. | Every worker has reachable private `/alive` and `/health`; bus/store failures and shutdown change readiness as specified. | Architecture test enumerating `AddServiceDefaults` worker projects and runtime probe tests per dependency type. |
| HLT-03 | High | `Products.gRPC/Program.cs`, `Shipping.gRPC/Program.cs`, `Stock.gRPC/Program.cs`, `PSP.Simulator/Program.cs` | These web processes call defaults but never map default endpoints; Products.gRPC also has an unresolved service dependency graph. Map endpoints and validate required service graphs/config at startup. | Both routes exist on the management endpoint and core service dependencies resolve before ready. | Startup smoke test that resolves every mapped gRPC service and calls both probes. |
| HLT-04 | Medium | `Gateway/Program.cs`, `Gateway/appsettings.json` | Gateway readiness must not query all destinations. Limit it to local route/auth option validation and process health. | All downstreams may be stopped while Gateway `/health` stays healthy; malformed route configuration prevents startup/readiness. | Test host with unreachable destinations and a separate invalid-config test. |
| HLT-05 | Medium | Shared defaults and all database/cache registrations in the matrix | There are no bounded, sanitized dependency checks. Provide common check factories with 1-second dependency and 2-second aggregate budgets. | Hung dependencies never hold a probe beyond budget; responses contain no endpoint, exception, or secret data. | Slow/failing fake check tests and response snapshot/secret-canary test. |
| HLT-06 | Medium | `src/AppHost/AppHost.cs`, `src/AppHost/Setup/**/*.cs`, Basket/Customers/Products gRPC programs | AppHost references sometimes exceed actual runtime usage, so startup ordering is not a reliable dependency inventory. Keep a tested process dependency manifest derived from registrations. | Every AppHost reference is classified as used, startup-only, or removable; matrix and code registrations cannot drift silently. | Architecture test comparing a checked-in dependency manifest to `Program.cs` and AppHost resource references. |

## Ownership split

Shared platform work owns route semantics, private worker listener, response writer, time budgets, common PostgreSQL/MongoDB/Redis/MassTransit check factories, shutdown state, and architecture tests. Bounded-context agents register only dependencies they own, decide whether a feature-scoped store is critical, correct invalid dependency graphs, and add context-specific outage tests. Gateway work must explicitly prove zero downstream fan-out.
