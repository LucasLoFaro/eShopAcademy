# Roadmap

Status: directional plan. The current implementation is described in the [architecture overview](../architecture/overview.md); detailed, evidence-backed work is tracked in [documentation-validation-gaps.md](documentation-validation-gaps.md).

## Current baseline

- The solution and active frontends exist and are orchestrated locally by Aspire.
- MassTransit already supports RabbitMQ and Azure Service Bus selection. RabbitMQ is the development default; Azure Service Bus is the non-development default.
- The order saga implements payment timeout scheduling, the principal success path, and several compensation paths.
- OpenTelemetry is present, but the production Application Insights export/deployment path is not.

## Near-term priorities

1. Establish production Azure infrastructure, identities, configuration, and deployment automation without replacing Aspire.
2. Provision and validate the Azure Service Bus topology and least-privilege access used by the existing transport implementation.
3. Implement the production Application Insights path, telemetry redaction, resource conventions, dashboards, alerts, and runbooks.
4. Add dependency-aware readiness and private management endpoints for workers.
5. Add retry/redelivery, inbox/outbox, idempotency, and provider reconciliation for critical workflows.
6. Resolve security and secrets-management blockers, including webhook verification, unsafe HTTP retries, service authorization, and production configuration validation.
7. Decide the disposition of projects and frontend prototypes that are outside the active solution/AppHost.

The detailed production-readiness specifications under [`production-readiness/`](production-readiness/README.md) are target contracts, not claims that these items are implemented.
