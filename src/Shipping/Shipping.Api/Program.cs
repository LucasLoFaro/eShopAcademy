using Domain.Shipping.Contracts.Requests;
using Microsoft.Extensions.Options;
using ServiceDefaults;
using Shipping.Application.Clients;
using Shipping.Application.Data;
using Shipping.Application.Options;
using Shipping.Application.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults()
       .WithSwagger()
       .WithMassTransit();

builder.Services.AddSingleton(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("shipping");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("The shipping MongoDB connection string is not configured.");
    }

    var databaseName = configuration["Shipping:Database"] ?? "shipping";

    return new ShippingDbContext(connectionString, databaseName);
});

builder.Services.AddScoped<IShippingStatusHistoryRepository, ShippingStatusHistoryRepository>();
builder.Services.AddScoped<IShippingStatusService, ShippingStatusService>();
builder.Services.Configure<ShippingProviderOptions>(builder.Configuration.GetSection("Shipping:Provider"));
builder.Services.AddHttpClient<IShippingProviderClient, ShippingProviderClient>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<ShippingProviderOptions>>().Value;

    if (string.IsNullOrWhiteSpace(options.BaseUrl))
    {
        throw new InvalidOperationException("The shipping provider base URL is not configured.");
    }

    client.BaseAddress = new Uri(options.BaseUrl);
});

var app = builder.Build();

app.MapPost("/api/shipping/webhook", async (
    ShippingStatusUpdateRequest? update,
    ILogger<Program> logger,
    IShippingStatusService service,
    CancellationToken cancellationToken) =>
{
    if (update is null)
    {
        logger.LogWarning("Received empty shipping status payload.");
        return Results.BadRequest();
    }

    if (string.IsNullOrWhiteSpace(update.Status))
    {
        logger.LogWarning("Shipping status payload received without a status for shipment {ShippingId}.",
            update.ShippingId);
        return Results.BadRequest();
    }

    await service.ProcessStatusUpdateAsync(update, cancellationToken);

    return Results.Accepted();
});

app.MapGet("/api/shipping/{orderId:guid}/provider-history", async (
    Guid orderId,
    IShippingStatusService service,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    var remoteHistory = await service.QueryProviderHistoryAsync(orderId, cancellationToken);

    if (remoteHistory.Count == 0)
    {
        logger.LogInformation("No shipping history returned by the provider for order {OrderId}.", orderId);
        return Results.NotFound();
    }

    return Results.Ok(remoteHistory);
});

app.MapGet("/api/shipping/{orderId:guid}/history", async (
    Guid orderId,
    IShippingStatusService service,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    var history = await service.GetHistoryAsync(orderId, cancellationToken);

    if (history.Count == 0)
    {
        logger.LogInformation("No shipping history found for order {OrderId}.", orderId);
        return Results.NotFound();
    }

    return Results.Ok(history);
});

app.MapGet("/api/shipping/{orderId:guid}/status", async (
    Guid orderId,
    IShippingStatusService service,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    var latest = await service.GetLatestStatusAsync(orderId, cancellationToken);

    if (latest is null)
    {
        logger.LogInformation("No shipping status recorded for order {OrderId}.", orderId);
        return Results.NotFound();
    }

    return Results.Ok(latest);
});

app.UseDefaultEndpoints();
app.Run();
