using Domain.Sellers.Contracts;
using Sellers.Application.Repositories;
using Sellers.Application.Services;
using ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults()
       .WithMassTransit()
       .WithSwagger();

builder.Services.AddSingleton<ISellerRepository, SellerRepository>();
builder.Services.AddScoped<ISellerService, SellerService>();

var app = builder.Build();

app.MapGet("/api/sellers", async (ISellerService sellerService, CancellationToken cancellationToken) =>
{
    var sellers = await sellerService.GetAllAsync(cancellationToken);
    return Results.Ok(sellers.Select(SellerResponse.FromSeller));
});

app.MapGet("/api/sellers/{sellerId:guid}", async (Guid sellerId, ISellerService sellerService, CancellationToken cancellationToken) =>
{
    var seller = await sellerService.GetByIdAsync(sellerId, cancellationToken);
    return seller is null ? Results.NotFound() : Results.Ok(SellerResponse.FromSeller(seller));
});

app.MapPost("/api/sellers", async (CreateSellerRequest request, ISellerService sellerService, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Name) ||
        string.IsNullOrWhiteSpace(request.Email) ||
        string.IsNullOrWhiteSpace(request.TaxId))
    {
        return Results.BadRequest("Name, Email and TaxId are required.");
    }

    var seller = await sellerService.CreateAsync(request, cancellationToken);
    return Results.Created($"/api/sellers/{seller.Id}", SellerResponse.FromSeller(seller));
});

app.MapPut("/api/sellers/{sellerId:guid}/status", async (
    Guid sellerId,
    UpdateSellerStatusRequest request,
    ISellerService sellerService,
    CancellationToken cancellationToken) =>
{
    var seller = await sellerService.UpdateStatusAsync(sellerId, request.Status, cancellationToken);
    return seller is null ? Results.NotFound() : Results.Ok(SellerResponse.FromSeller(seller));
});

app.MapPut("/api/sellers/{sellerId:guid}/products", async (
    Guid sellerId,
    AssignSellerProductsRequest request,
    ISellerService sellerService,
    CancellationToken cancellationToken) =>
{
    var seller = await sellerService.AssignPublishedProductsAsync(sellerId, request.ProductIds, cancellationToken);
    return seller is null ? Results.NotFound() : Results.Ok(SellerResponse.FromSeller(seller));
});

app.MapGet("/api/sellers/{sellerId:guid}/ledger", async (
    Guid sellerId,
    int? skip,
    int? take,
    ISellerService sellerService,
    CancellationToken cancellationToken) =>
{
    var seller = await sellerService.GetByIdAsync(sellerId, cancellationToken);
    if (seller is null)
    {
        return Results.NotFound();
    }

    var entries = seller.Ledger
        .OrderByDescending(entry => entry.CreatedAt)
        .Skip(Math.Max(skip ?? 0, 0))
        .Take(Math.Clamp(take ?? 50, 1, 500))
        .Select(SellerLedgerEntryResponse.FromEntry);

    return Results.Ok(entries);
});

app.UseDefaultEndpoints();
app.Run();
