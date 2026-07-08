using Domain.Sellers.Contracts;
using Microsoft.Identity.Web;
using Sellers.Application.Repositories;
using Sellers.Application.Services;
using ServiceDefaults;
using System.Security.Claims;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults()
       .WithMassTransit()
       .WithSwagger();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration, "EntraId");

builder.Services.Configure<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>(
    Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme,
    options =>
    {
        options.TokenValidationParameters.RoleClaimType = "roles";
    });

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

app.UseDefaultEndpoints();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

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
    if (!await CanAccessSellerAsync(user, sellerId, sellerService, cancellationToken))
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
    if (!await CanAccessSellerAsync(user, sellerId, sellerService, cancellationToken))
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
    if (!await CanAccessSellerAsync(user, sellerId, sellerService, cancellationToken))
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
    if (!await CanAccessSellerAsync(user, sellerId, sellerService, cancellationToken))
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
    if (!await CanAccessSellerAsync(user, sellerId, sellerService, cancellationToken))
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

app.MapPost("/api/sellers/register", async (
    RegisterSellerRequest request,
    ClaimsPrincipal user,
    ISellerService sellerService,
    CancellationToken cancellationToken) =>
{
    var oid = GetUserObjectId(user);
    if (string.IsNullOrEmpty(oid))
    {
        return Results.Unauthorized();
    }

    var existing = await sellerService.GetByIdentityAsync(oid, cancellationToken);
    if (existing is not null)
    {
        return Results.Conflict("Already registered as a seller.");
    }

    if (string.IsNullOrWhiteSpace(request.Name) ||
        string.IsNullOrWhiteSpace(request.Email) ||
        string.IsNullOrWhiteSpace(request.TaxId) ||
        string.IsNullOrWhiteSpace(request.DocumentUrl))
    {
        return Results.BadRequest("Name, Email, TaxId and DocumentUrl are required.");
    }

    var seller = await sellerService.RegisterAsync(oid, request, cancellationToken);
    return Results.Created($"/api/sellers/{seller.Id}", SellerResponse.FromSeller(seller));
}).RequireAuthorization("sellers-authenticated");

app.MapGet("/api/sellers/me", async (
    ClaimsPrincipal user,
    ISellerService sellerService,
    CancellationToken cancellationToken) =>
{
    var oid = GetUserObjectId(user);
    if (string.IsNullOrEmpty(oid))
    {
        return Results.Unauthorized();
    }

    var seller = await sellerService.GetByIdentityAsync(oid, cancellationToken);
    return seller is null ? Results.NotFound() : Results.Ok(SellerResponse.FromSeller(seller));
}).RequireAuthorization("sellers-authenticated");

app.MapPost("/api/sellers/analyze-document", (IFormFile document) =>
{
    // Hardcoded response - will be replaced with Azure Document Intelligence
    var result = new
    {
        Name = "Tech Solutions Ltd.",
        TaxId = "12-3456789",
        Email = "contact@techsolutions.com",
        Address = new
        {
            Street = "Innovation Drive",
            Number = "1250",
            AdditionalInformation = "Suite 400",
            ZipCode = "94025",
            City = "Menlo Park",
            State = "CA",
            Country = "United States"
        }
    };

    return Results.Ok(result);
}).RequireAuthorization("sellers-authenticated")
  .DisableAntiforgery();

app.Run();

static string? GetUserObjectId(ClaimsPrincipal user) =>
    user.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
    ?? user.FindFirst("oid")?.Value;

static async Task<bool> CanAccessSellerAsync(ClaimsPrincipal user, Guid sellerId, ISellerService sellerService, CancellationToken cancellationToken)
{
    if (user.IsInRole("admin") || user.HasClaim("roles", "admin") || user.HasClaim("permissions", "sellers:admin"))
    {
        return true;
    }

    // Check legacy seller_id claim
    var sellerClaimValue = user.FindFirst("seller_id")?.Value ?? user.FindFirst("sellerId")?.Value;
    if (Guid.TryParse(sellerClaimValue, out var claimSellerId) && claimSellerId == sellerId)
    {
        return true;
    }

    // Resolve seller by the user's identity object id
    var oid = GetUserObjectId(user);
    if (string.IsNullOrEmpty(oid))
    {
        return false;
    }

    var seller = await sellerService.GetByIdentityAsync(oid, cancellationToken);
    return seller is not null && seller.Id == sellerId;
}
