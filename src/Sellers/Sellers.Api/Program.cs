using Domain.Sellers.Contracts;
using Microsoft.Identity.Web;
using Sellers.Application.Repositories;
using Sellers.Application.Services;
using ServiceDefaults;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults()
       .WithMassTransit()
       .WithSwagger();

builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration, "EntraId");
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("sellers-admin", policy =>
        policy.RequireAssertion(ctx =>
            ctx.User.Identity?.IsAuthenticated == true &&
            (ctx.User.IsInRole("admin") ||
             ctx.User.HasClaim("roles", "admin") ||
             ctx.User.HasClaim("permissions", "sellers:admin"))))
    .AddPolicy("sellers-authenticated", policy => policy.RequireAuthenticatedUser());

builder.Services.AddSingleton<ISellerRepository, SellerRepository>();
builder.Services.AddScoped<ISellerService, SellerService>();

var app = builder.Build();

app.MapGet("/api/sellers", async (ISellerService sellerService, CancellationToken cancellationToken) =>
{
    var sellers = await sellerService.GetAllAsync(cancellationToken);
    return Results.Ok(sellers.Select(SellerResponse.FromSeller));
}).RequireAuthorization("sellers-admin");

app.MapGet("/api/sellers/{sellerId:guid}", async (
    Guid sellerId,
    ClaimsPrincipal user,
    ISellerService sellerService,
    CancellationToken cancellationToken) =>
{
    if (!CanAccessSeller(user, sellerId))
    {
        return Results.Forbid();
    }

    var seller = await sellerService.GetByIdAsync(sellerId, cancellationToken);
    return seller is null ? Results.NotFound() : Results.Ok(SellerResponse.FromSeller(seller));
}).RequireAuthorization("sellers-authenticated");

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
}).RequireAuthorization("sellers-admin");

app.MapPut("/api/sellers/{sellerId:guid}/status", async (
    Guid sellerId,
    UpdateSellerStatusRequest request,
    ClaimsPrincipal user,
    ISellerService sellerService,
    CancellationToken cancellationToken) =>
{
    if (!CanAccessSeller(user, sellerId))
    {
        return Results.Forbid();
    }

    var seller = await sellerService.UpdateStatusAsync(sellerId, request.Status, cancellationToken);
    return seller is null ? Results.NotFound() : Results.Ok(SellerResponse.FromSeller(seller));
}).RequireAuthorization("sellers-authenticated");

app.MapPut("/api/sellers/{sellerId:guid}/products", async (
    Guid sellerId,
    AssignSellerProductsRequest request,
    ClaimsPrincipal user,
    ISellerService sellerService,
    CancellationToken cancellationToken) =>
{
    if (!CanAccessSeller(user, sellerId))
    {
        return Results.Forbid();
    }

    var seller = await sellerService.AssignPublishedProductsAsync(sellerId, request.ProductIds, cancellationToken);
    return seller is null ? Results.NotFound() : Results.Ok(SellerResponse.FromSeller(seller));
}).RequireAuthorization("sellers-authenticated");

app.MapGet("/api/sellers/{sellerId:guid}/ledger", async (
    Guid sellerId,
    int? skip,
    int? take,
    ClaimsPrincipal user,
    ISellerService sellerService,
    CancellationToken cancellationToken) =>
{
    if (!CanAccessSeller(user, sellerId))
    {
        return Results.Forbid();
    }

    var seller = await sellerService.GetByIdAsync(sellerId, cancellationToken);
    if (seller is null)
    {
        return Results.NotFound();
    }

    var ledger = seller.Ledger
        .OrderByDescending(entry => entry.CreatedAt)
        .Skip(Math.Max(skip ?? 0, 0))
        .Take(Math.Clamp(take ?? 50, 1, 500))
        .Select(SellerLedgerEntryResponse.FromEntry);

    return Results.Ok(ledger);
}).RequireAuthorization("sellers-authenticated");

app.MapGet("/api/sellers/{sellerId:guid}/financial-summary", async (
    Guid sellerId,
    ClaimsPrincipal user,
    ISellerService sellerService,
    CancellationToken cancellationToken) =>
{
    if (!CanAccessSeller(user, sellerId))
    {
        return Results.Forbid();
    }

    var seller = await sellerService.GetByIdAsync(sellerId, cancellationToken);
    if (seller is null)
    {
        return Results.NotFound();
    }

    return Results.Ok(new SellerFinancialSummaryResponse(
        seller.Id,
        seller.AccumulatedSalesAmount,
        seller.AccumulatedCommissionsAmount,
        seller.AccumulatedSalesAmount - seller.AccumulatedCommissionsAmount,
        seller.Ledger.Count));
}).RequireAuthorization("sellers-authenticated");

app.UseAuthentication();
app.UseAuthorization();

app.UseDefaultEndpoints();
app.Run();

static bool CanAccessSeller(ClaimsPrincipal user, Guid sellerId)
{
    if (user.IsInRole("admin") || user.HasClaim("roles", "admin") || user.HasClaim("permissions", "sellers:admin"))
    {
        return true;
    }

    var sellerClaimValue = user.FindFirst("seller_id")?.Value ?? user.FindFirst("sellerId")?.Value;
    return Guid.TryParse(sellerClaimValue, out var claimSellerId) && claimSellerId == sellerId;
}
