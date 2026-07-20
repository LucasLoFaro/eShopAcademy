# Production deployment status

Status: intended architecture plus current repository evidence. This is not a runnable production deployment guide because the required infrastructure and pipeline are not present.

## Intended production shape

- Preserve Aspire as the application model and local-development orchestrator.
- Use Azure Service Bus for MassTransit messaging.
- Use Application Insights as the production observability destination, preferably through a single documented OpenTelemetry export path.
- Use managed identity/`DefaultAzureCredential`, Azure App Configuration, and Key Vault references for production configuration and secrets.

## Implemented application support

- Non-development messaging defaults to `AzureServiceBus`.
- `Messaging:AzureServiceBus:NamespaceUri` accepts an HTTPS or `sb` namespace URI.
- MassTransit uses `DefaultAzureCredential`, native Azure Service Bus scheduling, and the `Messaging:AzureServiceBus:CreateTopology` switch.
- Non-development hosts optionally load Azure App Configuration when `APPCONFIGURATION` is set and resolve Key Vault references with `DefaultAzureCredential`.
- Applications can export OpenTelemetry through OTLP when `OTEL_EXPORTER_OTLP_ENDPOINT` is set.

## Missing deployment artifacts

The repository contains no `azure.yaml`, Bicep/Terraform, Aspire publish output, production container topology, environment parameter contract, or deployment workflow. The only checked-in workflow builds and tests the solution. No Azure Service Bus namespace/entities/RBAC or Application Insights/collector configuration is defined.

Do not infer production readiness from the existence of the Azure Service Bus configurator. The application path exists; the infrastructure, security, validation, operations, and end-to-end proof do not.

See [documentation-validation-gaps.md](../plans/documentation-validation-gaps.md) for recommended implementation work and production blockers.

