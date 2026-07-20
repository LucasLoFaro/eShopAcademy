using Domain.Shipping.Contracts.Requests;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ServiceDefaults;
using Shipping.Application.Clients;
using Shipping.Application.Data;
using Shipping.Application.Options;
using Shipping.Application.Services;
using Shipping.Application;
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenTelemetry().WithMetrics(metrics => metrics.AddMeter(ShippingMetrics.MeterName));

builder.AddServiceDefaults()
       .WithSwagger()
       .WithMassTransit();

var connectionString = builder.Configuration.GetConnectionString("shipping");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("The shipping MongoDB connection string is not configured.");

var shippingDatabase = new ShippingDbContext(
    connectionString,
    builder.Configuration["Shipping:Database"] ?? "shipping");
builder.Services.AddSingleton(shippingDatabase);
builder.Services.AddHealthChecks().AddAsyncCheck(
    "shipping-database",
    async cancellationToken =>
    {
        await shippingDatabase.PingAsync(cancellationToken);
        return HealthCheckResult.Healthy();
    },
    tags: ["ready"],
    timeout: TimeSpan.FromSeconds(3));
builder.Services.AddProblemDetails();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
        .AllowAnyHeader()
        .AllowAnyMethod()));

builder.Services.AddScoped<IShippingStatusHistoryRepository, ShippingStatusHistoryRepository>();
builder.Services.AddScoped<IShippingStatusService, ShippingStatusService>();
builder.Services.AddOptions<ShippingProviderOptions>()
    .Bind(builder.Configuration.GetSection("Shipping:Provider"))
    .Validate(ShippingProviderOptions.IsValid, "Shipping provider BaseUrl must be an absolute HTTP(S) URI.")
    .ValidateOnStart();
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
app.UseExceptionHandler();
app.UseCors();

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
