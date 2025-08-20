$ErrorActionPreference = "Stop"
dotnet --info
dotnet restore src/eShopAcademy.sln
dotnet build src/eShopAcademy.sln -c Release --no-restore
dotnet test  src/eShopAcademy.sln -c Release --no-build
