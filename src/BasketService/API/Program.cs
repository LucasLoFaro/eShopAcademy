using Microsoft.AspNetCore.Mvc;
using Core.Domain.Entities;
using Data.Interfaces;
using Data;


var builder = WebApplication.CreateBuilder(args);
builder.Environment.ApplicationName = "basket.api";

builder.AddServiceDefaults()
       .AddSwagger();

//Inject services
builder.Services.AddSingleton<IDatabaseClient>(sp => new DatabaseClient(builder.Configuration.GetConnectionString("Redis")!));
builder.Services.AddTransient<IBasketCache, BasketCache>();

var app = builder.Build();
app.MapDefaultEndpoints();

app.MapGet("/basket/clientId", async Task<IResult> (Guid clientID, IBasketCache basketRepository) =>
{
    var basket = await basketRepository.GetBasketLoadedByClientId(clientID);
    return basket != null ? Results.Ok(basket) : Results.NotFound();
});

app.MapPost("/basket/clientId/add", async (Guid clientID, [FromBody] Item item, IBasketCache basketRepository) =>
{
    return await basketRepository.AddProductToBasket(clientID, item) ? Results.Ok() : Results.NotFound();
});

app.MapPost("/basket/clientId/remove", async (Guid clientID, [FromBody] Item item, IBasketCache basketRepository) =>
{
    return await basketRepository.RemoveProductFromBasket(clientID, item) ? Results.Ok() : Results.NotFound();
});

app.Run();