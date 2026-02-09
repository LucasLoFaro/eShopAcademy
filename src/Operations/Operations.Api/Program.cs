using Domain.Operations.Contracts;
using Operations.Application.Repositories;
using Operations.Application.Services;
using ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults()
       .WithMassTransit()
       .WithSwagger();

builder.Services.AddSingleton<IPackageRepository, PackageRepository>();
builder.Services.AddScoped<IPackageWorkflowService, PackageWorkflowService>();

var app = builder.Build();

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

app.UseDefaultEndpoints();
app.Run();
