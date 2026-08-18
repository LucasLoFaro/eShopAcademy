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

## Shipping signature secret contract

Shipping API, Shipping Service, and Shipping Simulator require `Shipping:SignatureSecret` at startup. Values shorter than 32 characters, empty values, and missing values fail startup with a key-only validation error.

For every non-development Shipping process:

1. Set `APPCONFIGURATION` to the Azure App Configuration resource name, without the `.azconfig.io` suffix.
2. Create the Azure App Configuration key `common:Shipping:SignatureSecret` with the null (`No Label`) label.
3. Make that entry an Azure Key Vault reference to a versionless secret URI such as `https://<vault>.vault.azure.net/secrets/<secret-name>`. Do not store the secret value in App Configuration.
4. Grant the workload managed identity read access to Azure App Configuration and permission to resolve that Key Vault secret. The repository uses `DefaultAzureCredential`; it does not define these identities or role assignments.

The shared configuration loader selects the null-label `common:*` keys, removes the `common:` prefix, and resolves the reference as `Shipping:SignatureSecret`. Using one common key ensures all three processes receive the same value.

To rotate without rebuilding, add a new enabled Key Vault secret version under the same secret name, leave the versionless App Configuration reference unchanged, and perform a controlled restart/rollout of all three Shipping processes. Verify that each process starts successfully, then disable the old version after the rollout. Automatic configuration refresh is not configured, so rotation requires a restart but no source change or image rebuild.

## Missing deployment artifacts

The repository contains no `azure.yaml`, Bicep/Terraform, Aspire publish output, production container topology, or deployment workflow. The only checked-in workflow builds and tests the solution. No App Configuration store, Key Vault secret, managed identity/RBAC, Azure Service Bus namespace/entities/RBAC, or Application Insights/collector configuration is defined. The Shipping contract above is application support, not evidence that those Azure resources are provisioned or that an external secret has been rotated.

Do not infer production readiness from the existence of the Azure Service Bus configurator. The application path exists; the infrastructure, security, validation, operations, and end-to-end proof do not.

See [documentation-validation-gaps.md](../plans/documentation-validation-gaps.md) for recommended implementation work and production blockers.
