# Production-readiness specifications

These documents combine validated current-state evidence with target contracts. They do not mean the target controls or Azure infrastructure are implemented.

- [Owned services production-readiness tech debt](owned-services-tech-debt.md) — authoritative status ledger, evidence policy, runtime baseline, and issue/PR traceability for all open readiness debt.
- [Messaging transports](messaging-transports.md) — accepted current application decision: MassTransit, RabbitMQ in Development, Azure Service Bus in production/non-development.
- [Messaging topology](messaging-topology.md) and [`messaging-topology.json`](messaging-topology.json) — current consumer, saga, endpoint, and command-routing contract.
- [Messaging regression](messaging-regression.md) — automated and runtime regression plan; only part is represented by current tests.
- [Message delivery semantics](message-delivery-semantics.md) — current risk inventory and future retry/idempotency/inbox/outbox standard.
- [Health endpoints](health-endpoints.md) — current endpoint inventory and future readiness contract.
- [Observability](observability.md) — current OpenTelemetry inventory and intended Application Insights architecture.
- [Security and configuration](security-and-configuration.md) — current risks and future production controls.
- [Shared platform foundation migration guide](shared-platform-foundation.md) — reusable APIs, pilot adoption, at-least-once delivery contract, and service-agent decisions.

The consolidated issue-ready summary is [documentation-validation-gaps.md](../documentation-validation-gaps.md).
