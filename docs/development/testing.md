# Build and test

The CI workflow in `.github/workflows/dotnet-ci.yml` restores, builds, and tests the solution on .NET 10.

From the repository root:

```powershell
dotnet restore src/eShopAcademy.sln
dotnet build src/eShopAcademy.sln -c Release --no-restore
dotnet test src/eShopAcademy.sln -c Release --no-build
```

The solution currently contains 65 projects, including 11 test projects. Four project files exist outside the solution. Independent validation found that Shipping gRPC, Stock Application, and the Stock gRPC client build, while `Orders.Messaging.csproj` fails because `OrderMessagingClient` no longer implements two members of the current `IOrderMessagingClient` contract. See [documentation-validation-gaps.md](../plans/documentation-validation-gaps.md#repository-membership-and-duplicate-frontend-need-an-ownership-decision).

The standalone Stock gRPC client is one of those four projects. With the Aspire AppHost running, its default target is the configured Stock gRPC endpoint at `http://localhost:8022`; override it with the first command-line argument or `STOCK_GRPC_URL`:

```powershell
dotnet run --project src/Stock/Tests/StockGrpcClient/Stock.Tests.csproj -- http://localhost:8022
```

## Frontends

```powershell
npm ci --prefix src/Frontend/eshop-web
npm run lint --prefix src/Frontend/eshop-web
npm run build --prefix src/Frontend/eshop-web

npm ci --prefix src/Frontend/eshop-sellers
npm run build --prefix src/Frontend/eshop-sellers
```

Only `eshop-web` defines a lint script. The repository has no shared Markdown linter, formatter, or link-check framework; documentation links are checked with a repository-local validation command during documentation maintenance rather than adding a new framework.

For runtime messaging and frontend regression, follow [messaging-regression.md](../plans/production-readiness/messaging-regression.md). Those scenarios require local services, credentials, and mutable test data and are not all represented in CI.
