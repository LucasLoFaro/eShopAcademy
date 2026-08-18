using Data;
using Data.Interfaces;
using Domain.Basket.Entities;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults()
    .WithSwagger();

builder.Services.AddProblemDetails();
builder.Services.AddBasketStorage(builder.Configuration);

var app = builder.Build();
app.UseExceptionHandler();
app.UseDefaultEndpoints();

app.MapGet("/basket/clientId", async Task<IResult> (
    Guid clientID,
    IBasketCache basketRepository,
    CancellationToken cancellationToken) =>
{
    var basket = await basketRepository.GetBasketLoadedByClientId(clientID, cancellationToken);
    return basket is not null ? Results.Ok(basket) : Results.NotFound();
});

app.MapPost("/basket/clientId/add", async (
    Guid clientID,
    [FromBody] Item item,
    IBasketCache basketRepository,
    CancellationToken cancellationToken) =>
    await basketRepository.AddProductToBasket(clientID, item, cancellationToken)
        ? Results.Ok()
        : Results.NotFound());

app.MapPost("/basket/clientId/remove", async (
    Guid clientID,
    [FromBody] Item item,
    IBasketCache basketRepository,
    CancellationToken cancellationToken) =>
    await basketRepository.RemoveProductFromBasket(clientID, item, cancellationToken)
        ? Results.Ok()
        : Results.NotFound());

app.Run();

public partial class Program;
