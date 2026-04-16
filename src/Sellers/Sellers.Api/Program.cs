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
    var seller = await sellerService.CreateAsync(request, cancellationToken);
    return Results.Created($"/api/sellers/{seller.Id}", SellerResponse.FromSeller(seller));
});

app.UseDefaultEndpoints();
app.Run();
