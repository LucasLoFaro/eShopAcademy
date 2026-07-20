# eShopAcademy

eShopAcademy is a .NET 10 microservices reference application. .NET Aspire 13.4 is the local orchestrator for the APIs, workers, Vite frontends, RabbitMQ, PostgreSQL, MongoDB, Redis, and supporting emulators.

## Architecture at a glance

- Aspire is the supported local-development workflow; Docker Compose is not a replacement workflow for this repository.
- MassTransit abstracts messaging. Development defaults to RabbitMQ; non-development environments default to Azure Service Bus and require an Azure Service Bus namespace.
- OpenTelemetry is registered centrally. Production Application Insights integration is intended but is not yet implemented or deployed.
- Production infrastructure, deployment automation, and several production-readiness controls remain planned work.

## Start locally

Install the .NET 10 SDK, Aspire CLI, a supported container runtime, and Node.js/npm. Then run:

```powershell
dotnet user-secrets set "Parameters:sendgrid-apikey" "<development-key>" --project src/AppHost/AppHost.csproj
aspire run --apphost src/AppHost/AppHost.csproj
```

Use the URLs shown by the Aspire dashboard instead of assuming fixed ports.

## Documentation

- [Documentation index](docs/README.md)
- [Current architecture](docs/architecture/overview.md)
- [Local development](docs/development/local-development.md)
- [Build and test](docs/development/testing.md)
- [Production deployment status](docs/deployment/production.md)
- [Messaging transport and topology](docs/plans/production-readiness/messaging-transports.md)
- [Validated gaps and production blockers](docs/plans/documentation-validation-gaps.md)
- [AI-agent and tool guidance](docs/agents/README.md)
