# GitHub Copilot instructions

Use [`docs/agents/project-guidance.md`](../docs/agents/project-guidance.md) as the shared project guidance and [`docs/architecture/overview.md`](../docs/architecture/overview.md) as the current architecture source.

Key constraints:

- Preserve .NET 10 and .NET Aspire; never replace Aspire with Docker Compose.
- Use MassTransit through `ServiceDefaults`: RabbitMQ for Development, Azure Service Bus for production/non-development.
- Follow existing project names, namespaces, folders, service-discovery registrations, and tests before creating files.
- Do not claim planned production infrastructure or Application Insights integration is implemented.
- Keep detailed documentation under `docs`; keep required tool entry points at their recognized root paths.
