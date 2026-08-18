using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Driver;
using Operations.Application.Repositories;
using Operations.Service.Consumers;
using ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("operations");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("The operations MongoDB connection string is not configured.");

var mongoClient = new MongoClient(connectionString);
var mongoDatabase = mongoClient.GetDatabase(builder.Configuration["Operations:Database"] ?? "operations");
var packageRepository = new PackageRepository(
    mongoDatabase,
    builder.Configuration["Operations:PackagesCollection"] ?? "packages");
builder.Services.AddSingleton<IMongoClient>(mongoClient);
builder.Services.AddSingleton(mongoDatabase);
builder.Services.AddSingleton<IPackageRepository>(packageRepository);

builder.AddServiceDefaults()
    .WithMassTransit(assemblies: typeof(PreparePackageCommandConsumer).Assembly);

builder.Services.AddHealthChecks().AddAsyncCheck(
    "operations-database",
    async cancellationToken =>
    {
        await packageRepository.PingAsync(cancellationToken);
        return HealthCheckResult.Healthy();
    },
    tags: ["ready"],
    timeout: TimeSpan.FromSeconds(3));

var app = builder.Build();
app.UseDefaultEndpoints();
app.Run();
