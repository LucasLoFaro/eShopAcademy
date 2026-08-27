param(
    [string]$ResourceGroup = "eShopAcademy",
    [string]$EnvironmentName = "eshopacademy-qa-env",
    [string]$ContainerAppName = "eshopacademy-products-api",
    [string]$Image = "ghcr.io/lucaslofaro/eshopacademy-products-api:qa-f955fe3ef7bd9787cd1d3c949be9be5c18bdeb08"
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
        service = "products-api"
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
                    name = "products-api"
                    image = $Image
                    resources = @{ cpu = 0.25; memory = "0.5Gi" }
                    env = @(
                        @{ name = "ASPNETCORE_ENVIRONMENT"; value = "QA" },
                        @{ name = "DOTNET_ENVIRONMENT"; value = "QA" },
                        @{ name = "ASPNETCORE_URLS"; value = "http://+:8080" },
                        @{ name = "AZURE_TOKEN_CREDENTIALS"; value = "ManagedIdentityCredential" },
                        @{ name = "ConnectionStrings__products"; secretRef = "documentdb-connection" },
                        @{ name = "Products__Database"; value = "products" },
                        @{ name = "Messaging__Transport"; value = "AzureServiceBus" },
                        @{ name = "Messaging__AzureServiceBus__NamespaceUri"; value = "https://eShopAcademy.servicebus.windows.net" },
                        @{ name = "Messaging__AzureServiceBus__CreateTopology"; value = "false" },
                        @{ name = "ContentSafety__Endpoint"; value = "https://eshopacademy-contentsafety.cognitiveservices.azure.com/" },
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
