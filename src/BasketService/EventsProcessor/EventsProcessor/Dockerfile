#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER app
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["BasketService/EventsProcessor/EventsProcessor/EventsProcessor.csproj", "BasketService/EventsProcessor/EventsProcessor/"]
COPY ["ProductCatalogService/Core/Domain/Domain.csproj", "ProductCatalogService/Core/Domain/"]
COPY ["BasketService/Data/Data.csproj", "BasketService/Data/"]
COPY ["BasketService/Domain/Domain.csproj", "BasketService/Domain/"]
RUN dotnet restore "./BasketService/EventsProcessor/EventsProcessor/EventsProcessor.csproj"
COPY . .
WORKDIR "/src/BasketService/EventsProcessor/EventsProcessor"
RUN dotnet build "./EventsProcessor.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./EventsProcessor.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "EventsProcessor.dll"].
