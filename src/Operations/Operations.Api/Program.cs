using Domain.Operations.Contracts;
using Operations.Application.Repositories;
using Operations.Application.Services;
using ServiceDefaults;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults()
       .WithMassTransit()
       .WithSwagger();

builder.Services.AddProblemDetails();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
        .AllowAnyHeader()
        .AllowAnyMethod()));
var packageRepository = new PackageRepository(builder.Configuration);
builder.Services.AddSingleton<IPackageRepository>(packageRepository);
builder.Services.AddSingleton<OperationsRequestStore>();
builder.Services.AddHealthChecks().AddAsyncCheck(
    "operations-database",
    async cancellationToken =>
    {
        await packageRepository.PingAsync(cancellationToken);
        return HealthCheckResult.Healthy();
    },
    tags: ["ready"],
    timeout: TimeSpan.FromSeconds(3));
builder.Services.AddScoped<IPackageWorkflowService, PackageWorkflowService>();

var app = builder.Build();
app.UseExceptionHandler();
app.UseCors();
app.Use(async (context, next) =>
{
    if (HttpMethods.IsPost(context.Request.Method)
        && context.Request.Path.StartsWithSegments("/api/operations"))
    {
        var key = context.Request.Headers["Idempotency-Key"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(key) || key.Length > 128)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(
                new { error = "A valid Idempotency-Key header is required." },
                context.RequestAborted);
            return;
        }

        var store = context.RequestServices.GetRequiredService<OperationsRequestStore>();
        var requestId = $"{context.Request.Path.Value?.ToLowerInvariant()}:{key}";
        if (!await store.TryBeginAsync(requestId, context.RequestAborted))
        {
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            await context.Response.WriteAsJsonAsync(
                new { error = "This operations request has already been accepted." },
                context.RequestAborted);
            return;
        }
    }

    await next(context);
});

app.MapGet("/api/operations/pending-packages", async (
    IPackageWorkflowService workflowService,
    CancellationToken cancellationToken) =>
{
    var packages = await workflowService.GetPendingPackagesAsync(cancellationToken);
    return Results.Ok(packages.Select(PackageResponse.FromPackage));
});

app.MapPost("/api/operations/orders/{orderId:guid}/start-processing", async (
    Guid orderId,
    StartPackageProcessingRequest request,
    IPackageWorkflowService workflowService,
    CancellationToken cancellationToken) =>
{
    var state = await workflowService.StartProcessingAsync(orderId, request, cancellationToken);
    return Results.Ok(PackageResponse.FromPackage(state));
});

app.MapPost("/api/operations/orders/{orderId:guid}/ready-for-pickup", async (
    Guid orderId,
    MarkOrderReadyRequest request,
    IPackageWorkflowService workflowService,
    CancellationToken cancellationToken) =>
{
    var state = await workflowService.MarkReadyForPickupAsync(orderId, request, cancellationToken);
    return Results.Ok(PackageResponse.FromPackage(state));
});

app.MapPost("/api/operations/orders/{orderId:guid}/report-issue", async (
    Guid orderId,
    ReportPackageProblemRequest request,
    IPackageWorkflowService workflowService,
    CancellationToken cancellationToken) =>
{
    var state = await workflowService.ReportProblemAsync(orderId, request, cancellationToken);
    return Results.Ok(PackageResponse.FromPackage(state));
});

// Seller-scoped endpoints (ABAC: X-Seller-Id header required)
app.MapGet("/api/operations/seller/pending-packages", async (
    HttpContext httpContext,
    IPackageWorkflowService workflowService,
    CancellationToken cancellationToken) =>
{
    if (!TryGetSellerId(httpContext, out var sellerId))
        return Results.BadRequest("Missing or invalid X-Seller-Id header.");

    var packages = await workflowService.GetPendingPackagesBySellerAsync(sellerId, cancellationToken);
    return Results.Ok(packages.Select(PackageResponse.FromPackage));
});

app.MapGet("/api/operations/seller/packages", async (
    int? limit,
    HttpContext httpContext,
    IPackageWorkflowService workflowService,
    CancellationToken cancellationToken) =>
{
    if (!TryGetSellerId(httpContext, out var sellerId))
        return Results.BadRequest("Missing or invalid X-Seller-Id header.");

    var packages = await workflowService.GetPackagesBySellerAsync(sellerId, Math.Clamp(limit ?? 20, 1, 100), cancellationToken);
    return Results.Ok(packages.Select(PackageResponse.FromPackage));
});

app.MapPost("/api/operations/seller/orders/{orderId:guid}/start-processing", async (
    Guid orderId,
    HttpContext httpContext,
    IPackageWorkflowService workflowService,
    CancellationToken cancellationToken) =>
{
    if (!TryGetSellerId(httpContext, out var sellerId))
        return Results.BadRequest("Missing or invalid X-Seller-Id header.");

    var package = await workflowService.StartProcessingBySellerAsync(orderId, sellerId, cancellationToken);
    if (package is null)
        return Results.Forbid();

    return Results.Ok(PackageResponse.FromPackage(package));
});

app.MapPost("/api/operations/seller/orders/{orderId:guid}/report-issue", async (
    Guid orderId,
    ReportPackageProblemRequest request,
    HttpContext httpContext,
    IPackageWorkflowService workflowService,
    CancellationToken cancellationToken) =>
{
    if (!TryGetSellerId(httpContext, out var sellerId))
        return Results.BadRequest("Missing or invalid X-Seller-Id header.");

    var package = await workflowService.ReportProblemBySellerAsync(orderId, sellerId, request, cancellationToken);
    if (package is null)
        return Results.Forbid();

    return Results.Ok(PackageResponse.FromPackage(package));
});

app.MapPost("/api/operations/seller/orders/{orderId:guid}/ready-for-pickup", async (
    Guid orderId,
    MarkOrderReadyRequest request,
    HttpContext httpContext,
    IPackageWorkflowService workflowService,
    CancellationToken cancellationToken) =>
{
    if (!TryGetSellerId(httpContext, out var sellerId))
        return Results.BadRequest("Missing or invalid X-Seller-Id header.");

    var package = await workflowService.MarkReadyForPickupBySellerAsync(orderId, sellerId, request, cancellationToken);
    if (package is null)
        return Results.Forbid();

    return Results.Ok(PackageResponse.FromPackage(package));
});

app.UseDefaultEndpoints();
app.Run();

static bool TryGetSellerId(HttpContext httpContext, out Guid sellerId)
{
    sellerId = Guid.Empty;
    var header = httpContext.Request.Headers["X-Seller-Id"].FirstOrDefault();
    return !string.IsNullOrWhiteSpace(header) && Guid.TryParse(header, out sellerId);
}
