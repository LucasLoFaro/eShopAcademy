param(
    [string]$ResourceGroup = "eShopAcademy",
    [string]$ContainerAppName = "eshopacademy-postgres",
    [ValidateSet(0, 1)]
    [int]$MinReplicas = 0
)

$ErrorActionPreference = "Stop"

$subscriptionId = az account show --query id --output tsv
if ($LASTEXITCODE -ne 0) { throw "Unable to resolve the active Azure subscription." }

$accessToken = az account get-access-token `
    --resource https://management.azure.com/ `
    --query accessToken `
    --output tsv
if ($LASTEXITCODE -ne 0) { throw "Unable to acquire an Azure management token." }

$resourceId = "/subscriptions/$subscriptionId/resourceGroups/$ResourceGroup/providers/Microsoft.App/containerApps/$ContainerAppName"
$initScript = "printf 'CREATE DATABASE orders;\nCREATE DATABASE orchestration;\n' > /docker-entrypoint-initdb.d/00-eshop.sql; exec docker-entrypoint.sh postgres"

$body = @{
    properties = @{
        template = @{
            containers = @(
                @{
                    name = "postgres"
                    image = "docker.io/library/postgres:17-alpine"
                    resources = @{
                        cpu = 0.25
                        memory = "0.5Gi"
                    }
                    env = @(
                        @{ name = "POSTGRES_USER"; value = "eshop" },
                        @{ name = "POSTGRES_PASSWORD"; secretRef = "pg-password" }
                    )
                    command = @("/bin/sh")
                    args = @("-c", $initScript)
                }
            )
            scale = @{
                minReplicas = $MinReplicas
                maxReplicas = 1
                rules = @(
                    @{
                        name = "tcp-scaler"
                        tcp = @{ metadata = @{ concurrentConnections = "5" } }
                    }
                )
            }
            volumes = @()
        }
    }
}

$headers = @{ Authorization = "Bearer $accessToken" }
$uri = "https://management.azure.com${resourceId}?api-version=2024-03-01"
$json = $body | ConvertTo-Json -Depth 20 -Compress
$response = Invoke-RestMethod `
    -Method Patch `
    -Uri $uri `
    -Headers $headers `
    -ContentType "application/json" `
    -Body $json

[pscustomobject]@{
    Name = $response.name
    LatestRevision = $response.properties.latestRevisionName
    ProvisioningState = $response.properties.provisioningState
}
