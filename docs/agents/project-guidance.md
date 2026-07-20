# Shared project guidance

## Architecture constraints

- Target .NET 10 and preserve the existing bounded-context/project structure.
- Preserve .NET Aspire. It is the supported local orchestrator; do not replace it with Docker Compose.
- Use MassTransit as the messaging abstraction. RabbitMQ is the Development default and Azure Service Bus is the non-development/production default.
- Keep broker-specific APIs inside `src/ServiceDefaults` and AppHost composition only.
- Preserve the intended Application Insights production destination while accurately describing the current generic OTLP-only implementation.
- Treat each service's persistence as owned by that service; do not introduce cross-service ORM access or shared databases.

## Working conventions

- Read [the architecture overview](../architecture/overview.md) and the relevant production-readiness specification before changing a cross-cutting concern.
- Start the AppHost through the Aspire CLI/IDE, not `dotnet run` and not Compose.
- Prefer existing `AddServiceDefaults`, `WithMassTransit`, service-discovery, and endpoint-registration patterns.
- Add Entity Framework migrations with `dotnet ef` when a model change genuinely requires one; do not hand-author generated migration files.
- Add or update focused tests for behavior changes. The standard solution commands are in [testing.md](../development/testing.md).
- Do not claim planned production infrastructure, Application Insights export, health readiness, or delivery guarantees are implemented until repository evidence and tests support that claim.

## Documentation conventions

- Keep the root README concise and put detailed project documentation under `docs`.
- Label documents as current, intended, planned, or historical.
- Tool-discovery files and directories remain at their required root-relative paths; reusable explanations belong here.
- Record architectural or infrastructure gaps in [documentation-validation-gaps.md](../plans/documentation-validation-gaps.md) instead of making speculative implementation changes.

