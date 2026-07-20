# Local development

## Prerequisites

- .NET SDK 10.0; the repository does not currently pin a patch SDK with `global.json`.
- Aspire CLI compatible with the AppHost's 13.4 SDK/packages.
- Docker Desktop, Docker Engine, or Podman available to Aspire for container resources.
- Node.js and npm for the Vite frontends.
- A development SendGrid API key, or a placeholder when email delivery is not being exercised.
- Azure developer credentials only for features that call Azure-hosted services such as Content Safety.

The repository uses `src/NuGet.config`. If restore requires the configured GitHub Packages source, authenticate that source with a package-readable token; do not commit the token.

## Configure the AppHost secret

The AppHost declares `sendgrid-apikey` as a secret Aspire parameter. Store it in the AppHost user-secrets store:

```powershell
dotnet user-secrets set "Parameters:sendgrid-apikey" "<development-key>" --project src/AppHost/AppHost.csproj
```

## Start the distributed application

From the repository root:

```powershell
aspire run --apphost src/AppHost/AppHost.csproj
```

Alternatively, set `src/AppHost/AppHost.csproj` as the startup project in the IDE. Do not run the AppHost with `dotnet run`, and do not introduce Docker Compose as a parallel local orchestration path.

The checked-in `src/aspire.config.json` also lets the Aspire CLI discover the AppHost when commands are run from `src`.

Use the Aspire dashboard or `aspire describe` for actual endpoint URLs. Although the AppHost requests familiar development ports, Aspire remains the source of truth for reachable URLs and resource state.

## Local resource model

| Resource | Purpose |
| --- | --- |
| `rabbit` | RabbitMQ development transport with management plugin and persistent volume |
| `postgres` | Orders and orchestration PostgreSQL databases; pgAdmin is enabled |
| `mongodb` | Products, stock, customers, shipping, operations, notifications, and sellers databases |
| `redis` | Basket cache and shipping-simulator state |
| `storage` | Azurite emulator; currently created but not connected to the Sellers `productimages` reference |
| `eshopacademy-gateway` | YARP gateway, requested on port 5200 |
| `eshopacademy-frontend` | Consumer Vite frontend, requested on port 5173 |
| `eshopacademy-sellers-frontend` | Seller Vite microfrontend, requested on port 5174 |

The bounded-context API ports requested in AppHost extensions are Products 8001, Stock 8002, Orders 8003, Basket 8004, Payments 8006, Shipping 8007, Customers 8008, Operations 8009, Sellers 8010, and Notifications 8011. gRPC endpoints request Products 8021, Stock 8022, and Payments 8026. The shipping and PSP simulators request 8027 and 8050. Prefer discovered URLs over these requested values.

## Messaging by environment

The Visual Studio AppHost profiles explicitly set `Messaging__Transport=RabbitMq`. The shared resolver also defaults Development to RabbitMQ and other environments to Azure Service Bus.

To exercise the Azure Service Bus registration outside production, set:

```powershell
$env:Messaging__Transport = "AzureServiceBus"
$env:Messaging__AzureServiceBus__NamespaceUri = "https://<namespace>.servicebus.windows.net"
```

`DefaultAzureCredential` is used. This repository does not provision the namespace, entities, or RBAC assignments.

## Stop and inspect

For a detached CLI session started with `aspire start`, use `aspire ps`, `aspire describe`, `aspire wait <resource>`, and `aspire stop`. Agent/worktree automation should add `--isolated --non-interactive` to `aspire start` and must stop the AppHost before running builds that could encounter file locks.

