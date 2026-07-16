# Messaging transports

Status: accepted for the production-readiness messaging foundation.

## Decision

MassTransit remains the application-level messaging abstraction. RabbitMQ is the local-development transport and Azure Service Bus is the production transport. Consumers, sagas, application services, and public message contracts remain broker-neutral; transport packages and broker-specific configuration live in `ServiceDefaults` and host composition roots only. No additional `IMessageBus` abstraction is introduced.

The broker is selected by `Messaging:Transport`, whose supported values are exactly `RabbitMq` and `AzureServiceBus`. The environment may provide a default (`RabbitMq` for Development, `AzureServiceBus` otherwise), but an explicit configuration value always wins so both paths can be built and tested in any environment.

## Configuration contract

```json
{
  "Messaging": {
    "Transport": "RabbitMq",
    "RabbitMq": {
      "ConnectionString": "amqp://guest:guest@localhost:5672"
    },
    "AzureServiceBus": {
      "NamespaceUri": "https://example.servicebus.windows.net",
      "CreateTopology": true
    }
  }
}
```

`RabbitMq:ConnectionString` may fall back to `ConnectionStrings:rabbit` for Aspire local development. `AzureServiceBus:NamespaceUri` is an absolute namespace URI and is authenticated with `DefaultAzureCredential`; a Service Bus connection string is neither accepted nor required. Options are validated when the host starts and may also be validated directly in tests.

`AzureServiceBus:CreateTopology=false` disables MassTransit publish topology deployment and automatic consume subscriptions. All queues, topics, and subscriptions in `messaging-topology.json` must already exist before such a host starts. Receive endpoints still open their named durable queue, so the later Bicep milestone must provision the complete manifest first.

## Transport configurators

- RabbitMQ owns its host URI, automatic local topology, and Quartz scheduler integration. Scheduler-enabled hosts register the Quartz consumers and use the durable `queue:quartz` scheduler endpoint.
- Azure Service Bus owns its namespace URI, `DefaultAzureCredential`, native Service Bus scheduler, and topology-creation switch.
- Shared registration owns consumer/saga discovery, kebab-case endpoint naming, service-specific endpoint callbacks, and transport dispatch only.
- Shared registration does not install a saga repository. Every saga host must select persistence explicitly.

The scheduler is opt-in per host. Orchestration opts in because `OrderStateMachine` schedules and unschedules `OrderExpiredEvent`; other hosts do not get scheduler infrastructure.

## Endpoint and topology policy

All endpoint names are explicit or derived through MassTransit's kebab-case formatter and must be identical on both transports. RabbitMQ exchanges/routing keys and Azure Service Bus entity APIs are forbidden outside `ServiceDefaults`. Runtime endpoint callbacks use MassTransit interfaces only.

RabbitMQ creates local topology automatically. Azure Service Bus may create topology at runtime during the transition period, or run against pre-provisioned entities when `CreateTopology=false`. The machine-readable manifest is the source contract for the later Bicep story; this story does not create Azure resources.

## Package decision

All packages whose ID is `MassTransit` or starts with `MassTransit.` use `MassTransitVersion` from the repository `Directory.Packages.props`. Version `8.5.10` is the latest stable Apache-2.0 release in the 8.x line and is used for the shared RabbitMQ, Azure Service Bus, Entity Framework, Quartz, and test surfaces. Its APIs were verified to support the required namespace/credential host overload, native Service Bus scheduler, Quartz scheduler, deterministic naming, and topology controls. The previous direct versions (8.5.4, 9.0.1, and 9.1.2) are removed without upgrading unrelated packages or introducing the MassTransit 9 commercial runtime license requirement.

## Guardrails

Tests must fail when MassTransit versions drift, endpoint names conflict, application assemblies reference RabbitMQ/Azure Service Bus APIs, either scheduler path disappears, a transport registration cannot be built without a broker connection, or a discovered consumer/saga cannot be configured.
