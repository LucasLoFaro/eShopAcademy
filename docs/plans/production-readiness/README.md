# Production-readiness specifications

These documents combine validated current-state evidence with target contracts. They do not mean the target controls or Azure infrastructure are implemented.

- [Messaging transports](messaging-transports.md) — accepted current application decision: MassTransit, RabbitMQ in Development, Azure Service Bus in production/non-development.
- [Messaging topology](messaging-topology.md) and [`messaging-topology.json`](messaging-topology.json) — current consumer, saga, endpoint, and command-routing contract.
- [Messaging regression](messaging-regression.md) — automated and runtime regression plan; only part is represented by current tests.
- [Message delivery semantics](message-delivery-semantics.md) — current risk inventory and future retry/idempotency/inbox/outbox standard.
- [Health endpoints](health-endpoints.md) — current endpoint inventory and future readiness contract.
- [Observability](observability.md) — current OpenTelemetry inventory and intended Application Insights architecture.
- [Security and configuration](security-and-configuration.md) — current risks and future production controls.

The consolidated issue-ready summary is [documentation-validation-gaps.md](../documentation-validation-gaps.md).

