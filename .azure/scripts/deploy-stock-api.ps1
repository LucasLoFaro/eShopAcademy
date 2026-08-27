param(
    [string]$ResourceGroup = "eShopAcademy",
    [string]$EnvironmentName = "eshopacademy-qa-env",
    [string]$ContainerAppName = "eshopacademy-stock-api",
    [string]$Image = "ghcr.io/lucaslofaro/eshopacademy-stock-api:qa-83914598eb1eaea1fc33e2efd77bf36e59134b0e"
)

$ErrorActionPreference = "Stop"

$subscriptionId = az account show --query id --output tsv
if ($LASTEXITCODE -ne 0) { throw "Unable to resolve the active Azure subscription." }

$accessToken = az account get-access-token `
    --resource https://management.azure.com/ `
    --query accessToken `
    --output tsv
if ($LASTEXITCODE -ne 0) { throw "Unable to acquire an Azure management token." }

$documentDbConnection = az keyvault secret show `
    --vault-name eShopAcademy `
    --name DocumentDB-ConnectionString `
    --query value `
    --output tsv
if ($LASTEXITCODE -ne 0) { throw "Unable to read the DocumentDB connection from Key Vault." }

$applicationInsightsConnection = az monitor app-insights component show `
    --app eshopacademy-qa-insights `
    --resource-group $ResourceGroup `
    --query connectionString `
    --output tsv
if ($LASTEXITCODE -ne 0) { throw "Unable to read the Application Insights connection string." }

$environmentId = "/subscriptions/$subscriptionId/resourceGroups/$ResourceGroup/providers/Microsoft.App/managedEnvironments/$EnvironmentName"
$resourceId = "/subscriptions/$subscriptionId/resourceGroups/$ResourceGroup/providers/Microsoft.App/containerApps/$ContainerAppName"

$body = @{
    location = "East US"
    identity = @{ type = "SystemAssigned" }
    tags = @{
        environment = "qa"
        workload = "eShopAcademy"
        service = "stock-api"
        owner = "lucaslofaro"
        "managed-by" = "manual"
    }
    properties = @{
        managedEnvironmentId = $environmentId
        configuration = @{
            activeRevisionsMode = "Single"
            ingress = @{
                external = $true
                targetPort = 8080
                transport = "auto"
                allowInsecure = $false
                traffic = @(@{ latestRevision = $true; weight = 100 })
            }
            secrets = @(
                @{ name = "documentdb-connection"; value = $documentDbConnection }
            )
        }
        template = @{
            containers = @(
                @{
                    name = "stock-api"
                    image = $Image
                    resources = @{ cpu = 0.25; memory = "0.5Gi" }
                    env = @(
                        @{ name = "ASPNETCORE_ENVIRONMENT"; value = "QA" },
                        @{ name = "DOTNET_ENVIRONMENT"; value = "QA" },
                        @{ name = "ASPNETCORE_URLS"; value = "http://+:8080" },
                        @{ name = "AZURE_TOKEN_CREDENTIALS"; value = "ManagedIdentityCredential" },
                        @{ name = "ConnectionStrings__stock"; secretRef = "documentdb-connection" },
                        @{ name = "Messaging__Transport"; value = "AzureServiceBus" },
                        @{ name = "Messaging__AzureServiceBus__NamespaceUri"; value = "https://eShopAcademy.servicebus.windows.net" },
                        @{ name = "Messaging__AzureServiceBus__CreateTopology"; value = "false" },
                        @{ name = "APPLICATIONINSIGHTS_CONNECTION_STRING"; value = $applicationInsightsConnection },
                        @{ name = "Telemetry__SamplingRatio"; value = "0.1" }
                    )
                    probes = @(
                        @{
                            type = "Liveness"
                            httpGet = @{ path = "/alive"; port = 8080; scheme = "HTTP" }
                            initialDelaySeconds = 60
                            periodSeconds = 10
                            timeoutSeconds = 3
                            failureThreshold = 3
                        },
                        @{
                            type = "Readiness"
                            httpGet = @{ path = "/health"; port = 8080; scheme = "HTTP" }
                            initialDelaySeconds = 60
                            periodSeconds = 10
                            timeoutSeconds = 3
                            failureThreshold = 6
                        }
                    )
                }
            )
            scale = @{
                minReplicas = 0
                maxReplicas = 1
                rules = @(
                    @{
                        name = "http-scaler"
                        http = @{ metadata = @{ concurrentRequests = "10" } }
                    }
                )
            }
        }
    }
}

$headers = @{ Authorization = "Bearer $accessToken" }
$uri = "https://management.azure.com${resourceId}?api-version=2024-03-01"
$json = $body | ConvertTo-Json -Depth 30 -Compress
Invoke-RestMethod `
    -Method Put `
    -Uri $uri `
    -Headers $headers `
    -ContentType "application/json" `
    -Body $json | Out-Null

Write-Output "Submitted $ContainerAppName with image $Image"
